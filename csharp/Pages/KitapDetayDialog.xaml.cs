using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using KutuphaneOtomasyon;
using KutuphaneOtomasyon.Pages;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitapDetayDialog : Window
    {
        private int _kitapId;

        public KitapDetayDialog()
        {
            InitializeComponent();
        }

        public KitapDetayDialog(int kitapId) : this()
        {
            _kitapId = kitapId;
            Loaded += (s, e) => LoadKitapDetay();
        }

        private void LoadKitapDetay()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // 1. Kitap Bilgilerini Getir
                using var cmdKitap = new NpgsqlCommand("SELECT * FROM Kitaplar WHERE KitapID = @id", conn);
                cmdKitap.Parameters.AddWithValue("@id", _kitapId);
                
                using (var reader = cmdKitap.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtBaslik.Text = reader["Baslik"].ToString();
                        txtYazar.Text = reader["Yazar"].ToString();
                        
                        // Tür ve Yıl birleşik
                        string yil = reader["YayinYili"]?.ToString();
                        // Tür adı gelmediği için şimdilik sadece yıl yazıyoruz, JOIN ile TurAdi alınabilir
                        txtTur.Text = string.IsNullOrEmpty(yil) ? "Bilinmiyor" : $"{yil}"; 
                        
                        // txtAciklama XAML'da yok demiştik ama kontrol edelim, eğer yoksa hata verir.
                        // XAML'da tanımı yok, o yüzden bu satırı siliyorum şimdilik.
                        
                        // Kitap Kapak Resmi (Placeholder - Renkli harf)
                        string baslik = txtBaslik.Text;
                        txtKapakHarf.Text = string.IsNullOrEmpty(baslik) ? "?" : baslik.Substring(0, 1).ToUpper();
                    }
                }

                // 2. Yorumları Getir
                // 2. Yorumları Getir
                using var cmdYorumlar = new NpgsqlCommand(@"
                    SELECT d.DegerlendirmeID, d.UyeID, d.Puan, d.Yorum, d.Tarih, k.AdSoyad 
                    FROM Degerlendirmeler d 
                    JOIN Kullanicilar k ON d.UyeID = k.KullaniciID 
                    WHERE d.KitapID = @id 
                    ORDER BY d.Tarih DESC", conn);
                cmdYorumlar.Parameters.AddWithValue("@id", _kitapId);
                
                var yorumlar = new List<YorumItem>();
                using (var reader = cmdYorumlar.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int degerlendirmeId = reader.GetInt32(0);
                        int uyeId = reader.GetInt32(1);
                        int currentUser = CurrentSession.UserId ?? 0;
                        string role = CurrentSession.Rol ?? "";
                        
                        // Admin veya kendi yorumu ise silme butonu görünsün
                        var isVisible = (role == "Yonetici" || currentUser == uyeId) 
                            ? Visibility.Visible : Visibility.Collapsed;

                        yorumlar.Add(new YorumItem
                        {
                            DegerlendirmeID = degerlendirmeId,
                            UyeID = uyeId,
                            Puan = reader.GetInt32(2),
                            Yorum = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Tarih = reader.GetDateTime(4),
                            AdSoyad = reader.GetString(5),
                            IsDeleteVisible = isVisible
                        });
                    }
                }
                lstYorumlar.ItemsSource = yorumlar;

                // 3. Ortalama Puanı Getir
                using var cmdAvg = new NpgsqlCommand("SELECT COUNT(*), AVG(CAST(Puan AS FLOAT)) FROM Degerlendirmeler WHERE KitapID = @id", conn);
                cmdAvg.Parameters.AddWithValue("@id", _kitapId);
                
                using (var readerAvg = cmdAvg.ExecuteReader())
                {
                    if (readerAvg.Read())
                    {
                        int count = readerAvg.GetInt32(0);
                        if (count > 0)
                        {
                            double avg = readerAvg.GetDouble(1);
                            txtOrtalamaPuan.Text = $"{avg:0.0} / 5 ({count} Değerlendirme)";
                        }
                        else
                        {
                            txtOrtalamaPuan.Text = "Henüz değerlendirilmedi";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RatingBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtUserPuan != null)
                txtUserPuan.Text = $"{(int)e.NewValue}/5";
        }

        private void Gonder_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSession.UserId == null)
            {
                MessageBox.Show("Yorum yapmak için tekrar giriş yapmalısınız!", "Oturum Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int puan = (int)ratingUser.Value;
            string yorum = txtYorum.Text.Trim();

            if (puan == 0)
            {
                MessageBox.Show("Lütfen puan veriniz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO Degerlendirmeler (KitapID, UyeID, Puan, Yorum, Tarih)
                    VALUES (@kitap, @uye, @puan, @yorum, NOW())", conn);
                
                cmd.Parameters.AddWithValue("@kitap", _kitapId);
                cmd.Parameters.AddWithValue("@uye", CurrentSession.UserId);
                cmd.Parameters.AddWithValue("@puan", puan);
                cmd.Parameters.AddWithValue("@yorum", string.IsNullOrEmpty(yorum) ? DBNull.Value : yorum);
                
                cmd.ExecuteNonQuery();
                
                MessageBox.Show("Değerlendirmeniz kaydedildi!", "Teşekkürler", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Ekranı güncelle
                txtYorum.Clear();
                ratingUser.Value = 0;
                LoadKitapDetay(); // Listeyi yenile
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void YorumSil_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is YorumItem item)
            {
                if (MessageBox.Show("Yorumu silmek istediğinize emin misiniz?", "Silme Onayı", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;
                
                try
                {
                    var result = await ApiService.DeleteDegerlendirmeAsync(item.DegerlendirmeID);
                    if (result != null && result.Success)
                    {
                        MessageBox.Show("Yorum silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadKitapDetay();
                    }
                    else
                    {
                        // API entegrasyonu yoksa veya hata verdiyse SQL ile deneyelim (yedek plan)
                        // Uyumluluk için doğrudan SQL ile silmeyi de buraya ekleyebilirim ama
                        // API servisine güvendiğimiz için şimdilik sadece hata mesajı verelim.
                        MessageBox.Show(result?.Mesaj ?? "Silme işlemi başarısız! (Yetki hatası olabilir)", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    
    public class YorumItem
    {
        public int DegerlendirmeID { get; set; }
        public int UyeID { get; set; }
        public int Puan { get; set; }
        public string Yorum { get; set; } = "";
        public DateTime Tarih { get; set; }
        public string AdSoyad { get; set; } = "";
        public Visibility IsDeleteVisible { get; set; } = Visibility.Collapsed;
    }
}
