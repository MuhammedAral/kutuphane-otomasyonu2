using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Media;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitapDetayDialog : Window
    {
        private readonly int _kitapId;
        
        public KitapDetayDialog(int kitapId)
        {
            InitializeComponent();
            _kitapId = kitapId;
            
            Loaded += (s, e) =>
            {
                DarkModeHelper.EnableDarkMode(this);
                LoadKitapDetay();
            };
        }
        
        private void LoadKitapDetay()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                // Kitap bilgilerini al
                var cmd = new SqlCommand(@"
                    SELECT k.*, t.TurAdi,
                           (SELECT COUNT(*) FROM OduncIslemleri WHERE KitapID = k.KitapID AND IadeTarihi IS NULL) as Oduncte
                    FROM Kitaplar k
                    LEFT JOIN KitapTurleri t ON k.TurID = t.TurID
                    WHERE k.KitapID = @id", conn);
                cmd.Parameters.AddWithValue("@id", _kitapId);
                
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtBaslik.Text = reader["Baslik"]?.ToString() ?? "-";
                    txtYazar.Text = reader["Yazar"]?.ToString() ?? "-";
                    txtYil.Text = reader["YayinYili"]?.ToString() ?? "-";
                    txtStok.Text = reader["MevcutAdet"]?.ToString() ?? "0";
                    txtOduncte.Text = reader["Oduncte"]?.ToString() ?? "0";
                    txtISBN.Text = reader["ISBN"]?.ToString() ?? "-";
                    txtTur.Text = reader["TurAdi"]?.ToString() ?? "-";
                    txtRaf.Text = reader["RafNo"]?.ToString() ?? "-";
                    txtSira.Text = reader["SiraNo"]?.ToString() ?? "-";
                }
                reader.Close();
                
                // Son ödünç işlemleri
                cmd = new SqlCommand(@"
                    SELECT TOP 5 u.AdSoyad, o.OduncTarihi,
                           CASE WHEN o.IadeTarihi IS NULL THEN 'Ödünçte' ELSE 'İade Edildi' END as Durum,
                           CASE WHEN o.IadeTarihi IS NULL THEN 1 ELSE 0 END as IsOduncte
                    FROM OduncIslemleri o
                    INNER JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
                    WHERE o.KitapID = @id
                    ORDER BY o.OduncTarihi DESC", conn);
                cmd.Parameters.AddWithValue("@id", _kitapId);
                
                var sonOduncler = new List<dynamic>();
                using var reader2 = cmd.ExecuteReader();
                while (reader2.Read())
                {
                    var isOduncte = Convert.ToInt32(reader2["IsOduncte"]) == 1;
                    sonOduncler.Add(new
                    {
                        AdSoyad = reader2["AdSoyad"].ToString(),
                        Tarih = Convert.ToDateTime(reader2["OduncTarihi"]).ToString("dd/MM/yyyy"),
                        Durum = reader2["Durum"].ToString(),
                        DurumRenk = isOduncte ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    });
                }
                icSonOduncler.ItemsSource = sonOduncler;
                txtNoData.Visibility = sonOduncler.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
