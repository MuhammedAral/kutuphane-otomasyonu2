using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitapDialog : Window
    {
        private readonly int? _kitapId;
        
        public KitapDialog(int? kitapId = null)
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
            _kitapId = kitapId;
            LoadTurler();
            
            if (_kitapId.HasValue)
            {
                txtTitle.Text = "📖 Kitap Düzenle";
                LoadKitap();
            }
        }
        
        private void LoadTurler()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var adapter = new SqlDataAdapter("SELECT TurID, TurAdi FROM KitapTurleri", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            cmbTur.ItemsSource = dt.DefaultView;
        }
        
        private void LoadKitap()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Kitaplar WHERE KitapID = @id", conn);
            cmd.Parameters.AddWithValue("@id", _kitapId);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                txtBaslik.Text = reader["Baslik"].ToString();
                txtYazar.Text = reader["Yazar"].ToString();
                txtYil.Text = reader["YayinYili"]?.ToString();
                txtStok.Text = reader["StokAdedi"].ToString();
                txtBarkod.Text = reader["Barkod"]?.ToString();
                txtRaf.Text = reader["RafNo"]?.ToString();
                txtSira.Text = reader["SiraNo"]?.ToString();
                // Barkod ve ISBN textboxları kaldırıldı
                txtAciklama.Text = reader["Aciklama"]?.ToString();
                cmbTur.SelectedValue = reader["TurID"];
            }
        }
        
        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaslik.Text) || string.IsNullOrWhiteSpace(txtYazar.Text))
            {
                MessageBox.Show("Lütfen zorunlu alanları doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string barkod = txtBarkod.Text.Trim();
            if (!string.IsNullOrEmpty(barkod) && barkod.Length != 13)
            {
                MessageBox.Show("Barkod 13 haneli olmalıdır!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(txtYil.Text) && (txtYil.Text.Length != 4 || !int.TryParse(txtYil.Text, out _)))
            {
                MessageBox.Show("Yayın yılı 4 haneli bir sayı olmalıdır (Örn: 2023)!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                SqlCommand cmd;
                if (_kitapId.HasValue)
                {
                    cmd = new SqlCommand(@"UPDATE Kitaplar SET 
                        Baslik = @baslik, Yazar = @yazar, YayinYili = @yil, 
                        TurID = @tur, StokAdedi = @stok, MevcutAdet = @stok, RafNo = @raf,
                        SiraNo = @sira, Barkod = @barkod, Aciklama = @aciklama
                        WHERE KitapID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", _kitapId);
                }
                else
                {
                    // Yeni kayıtta ISBN otomatik oluşturulur, Barkod kullanıcıdan gelir
                    cmd = new SqlCommand(@"INSERT INTO Kitaplar 
                        (Baslik, Yazar, ISBN, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo, SiraNo, Barkod, Aciklama)
                        VALUES (@baslik, @yazar, @isbn, @yil, @tur, @stok, @stok, @raf, @sira, @barkod, @aciklama)", conn);
                        
                    // Otomatik ISBN (Kullanıcı görmez, veritabanı hata vermesin diye)
                    string autoIsbn = Guid.NewGuid().ToString("N").Substring(0, 13).ToUpper();
                    cmd.Parameters.AddWithValue("@isbn", autoIsbn);
                }
                
                cmd.Parameters.AddWithValue("@baslik", txtBaslik.Text.Trim());
                cmd.Parameters.AddWithValue("@yazar", txtYazar.Text.Trim());
                cmd.Parameters.AddWithValue("@yil", int.TryParse(txtYil.Text, out var yil) ? yil : DBNull.Value);
                cmd.Parameters.AddWithValue("@tur", cmbTur.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@stok", int.TryParse(txtStok.Text, out var stok) ? stok : 1);
                cmd.Parameters.AddWithValue("@raf", string.IsNullOrEmpty(txtRaf.Text) ? DBNull.Value : txtRaf.Text);
                cmd.Parameters.AddWithValue("@sira", string.IsNullOrEmpty(txtSira.Text) ? DBNull.Value : txtSira.Text);
                cmd.Parameters.AddWithValue("@barkod", string.IsNullOrEmpty(txtBarkod.Text) ? DBNull.Value : txtBarkod.Text);
                cmd.Parameters.AddWithValue("@aciklama", string.IsNullOrEmpty(txtAciklama.Text) ? DBNull.Value : txtAciklama.Text);
                
                try 
                {
                    cmd.ExecuteNonQuery();
                    DialogResult = true;
                    Close();
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    // Eğer Barkod çakışırsa ve boş değilse uyarı ver
                    if (!string.IsNullOrEmpty(txtBarkod.Text))
                        MessageBox.Show("Bu barkod numarası zaten kayıtlı!", "Mükerrer Kayıt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        // Eğer boş barkod çakışıyorsa (Unique NULL sorunu), otomatik barkod ata ve tekrar dene
                        MessageBox.Show("Sistem hatası: Otomatik barkod oluşturulamadı. Lütfen manuel bir barkod giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt yapılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();
    }
}
