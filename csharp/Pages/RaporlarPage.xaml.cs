using Microsoft.Data.SqlClient;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class RaporlarPage : Page
    {
        public RaporlarPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadStats();
        }
        
        private void LoadStats()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var gecikmeUcreti = DatabaseHelper.GetGecikmeUcreti();
                
                // Toplam kitap
                using (var cmd = new SqlCommand("SELECT ISNULL(SUM(MevcutAdet), 0) FROM Kitaplar", conn))
                    txtToplamKitap.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                
                // Toplam üye
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE Rol = 'Uye'", conn))
                    txtToplamUye.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                
                // Aktif ödünç
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL", conn))
                    txtAktifOdunc.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                
                // Geciken
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL AND BeklenenIadeTarihi < GETDATE()", conn))
                    txtGeciken.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                
                // Bu ay ödünç verilen
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE MONTH(OduncTarihi) = MONTH(GETDATE()) AND YEAR(OduncTarihi) = YEAR(GETDATE())", conn))
                    txtBuAyOdunc.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                
                // Bu ay iade alınan
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE MONTH(IadeTarihi) = MONTH(GETDATE()) AND YEAR(IadeTarihi) = YEAR(GETDATE())", conn))
                    txtBuAyIade.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                
                // Toplam gecikme ücreti
                using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(DATEDIFF(DAY, BeklenenIadeTarihi, GETDATE())), 0)
                    FROM OduncIslemleri 
                    WHERE IadeTarihi IS NULL AND BeklenenIadeTarihi < GETDATE()", conn))
                {
                    var toplamGun = Convert.ToInt32(cmd.ExecuteScalar());
                    txtToplamGecikme.Text = $"₺{(toplamGun * gecikmeUcreti):F0}";
                }
                
                // En çok ödünç alınan kitaplar
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 5 k.Baslik, COUNT(*) as Sayi
                    FROM OduncIslemleri o
                    INNER JOIN Kitaplar k ON o.KitapID = k.KitapID
                    GROUP BY k.Baslik
                    ORDER BY COUNT(*) DESC", conn))
                {
                    var topKitaplar = new List<dynamic>();
                    using var reader = cmd.ExecuteReader();
                    int sira = 1;
                    while (reader.Read())
                    {
                        topKitaplar.Add(new { Sira = sira++, Baslik = reader["Baslik"].ToString(), Sayi = Convert.ToInt32(reader["Sayi"]) });
                    }
                    icTopKitaplar.ItemsSource = topKitaplar;
                }
                
                // En aktif üyeler
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 5 u.AdSoyad, COUNT(*) as Sayi
                    FROM OduncIslemleri o
                    INNER JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
                    GROUP BY u.AdSoyad
                    ORDER BY COUNT(*) DESC", conn))
                {
                    var topUyeler = new List<dynamic>();
                    using var reader = cmd.ExecuteReader();
                    int sira = 1;
                    while (reader.Read())
                    {
                        topUyeler.Add(new { Sira = sira++, AdSoyad = reader["AdSoyad"].ToString(), Sayi = Convert.ToInt32(reader["Sayi"]) });
                    }
                    icTopUyeler.ItemsSource = topUyeler;
                }
            }
            catch (Exception)
            {
                // Raporlar yüklenemezse sayfa boş kalır, kritik hata değil
            }
        }
    }
}
