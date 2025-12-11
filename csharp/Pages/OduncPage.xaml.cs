using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class OduncPage : Page
    {
        private bool _isLoaded = false;
        
        public OduncPage()
        {
            InitializeComponent();
            Loaded += (s, e) => { _isLoaded = true; LoadOduncler(); LoadStats(); };
        }
        
        private void LoadStats()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var gecikmeUcreti = DatabaseHelper.GetGecikmeUcreti();
                
                // Aktif ödünç sayısı
                using var cmdAktif = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL", conn);
                txtAktifOdunc.Text = cmdAktif.ExecuteScalar()?.ToString() ?? "0";
                
                // Geciken sayısı ve toplam ücret
                using var cmdGeciken = new SqlCommand(@"
                    SELECT COUNT(*) as Geciken, 
                           ISNULL(SUM(DATEDIFF(DAY, BeklenenIadeTarihi, GETDATE())), 0) as ToplamGun
                    FROM OduncIslemleri 
                    WHERE IadeTarihi IS NULL AND BeklenenIadeTarihi < GETDATE()", conn);
                
                using var reader = cmdGeciken.ExecuteReader();
                if (reader.Read())
                {
                    txtGeciken.Text = reader["Geciken"]?.ToString() ?? "0";
                    var toplamGun = Convert.ToInt32(reader["ToplamGun"]);
                    txtToplamUcret.Text = $"₺{(toplamGun * gecikmeUcreti):F2}";
                }
                reader.Close();
                
                // Bugün iade edilenler
                using var cmdBugun = new SqlCommand(@"
                    SELECT COUNT(*) FROM OduncIslemleri 
                    WHERE CAST(IadeTarihi AS DATE) = CAST(GETDATE() AS DATE)", conn);
                txtBugunIade.Text = cmdBugun.ExecuteScalar()?.ToString() ?? "0";
            }
            catch (Exception)
            {
                // İstatistikler kritik değil, sayfa yüklenmeye devam eder
            }
        }
        
        private void LoadOduncler()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var gecikmeUcreti = DatabaseHelper.GetGecikmeUcreti();
                
                var query = @"
                    SELECT o.IslemID, k.Baslik, u.AdSoyad, o.OduncTarihi, o.BeklenenIadeTarihi, o.IadeTarihi,
                           CASE WHEN o.IadeTarihi IS NULL THEN 'Ödünçte' ELSE 'İade Edildi' END as Durum,
                           CASE 
                               WHEN o.IadeTarihi IS NULL AND o.BeklenenIadeTarihi < GETDATE() 
                               THEN DATEDIFF(DAY, o.BeklenenIadeTarihi, GETDATE()) 
                               ELSE 0 
                           END as GecikmeGun
                    FROM OduncIslemleri o
                    INNER JOIN Kitaplar k ON o.KitapID = k.KitapID
                    INNER JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
                    WHERE 1=1"; // WHERE 1=1 ekledim ki diğer koşulları AND ile ekleyebileyim
                
                if (rbOdunc?.IsChecked == true)
                    query += " AND o.IadeTarihi IS NULL";
                else if (rbIade?.IsChecked == true)
                    query += " AND o.IadeTarihi IS NOT NULL";
                else if (rbGeciken?.IsChecked == true)
                    query += " AND o.IadeTarihi IS NULL AND o.BeklenenIadeTarihi < GETDATE()";

                // Arama Filtresi
                if (txtSearch != null && !string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    query += " AND (k.Baslik LIKE @search OR u.AdSoyad LIKE @search)";
                }
                
                query += " ORDER BY o.IslemID DESC";
                
                var adapter = new SqlDataAdapter(query, conn);

                if (txtSearch != null && !string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@search", $"%{txtSearch.Text}%");
                }
                
                var dt = new DataTable();
                adapter.Fill(dt);
                
                // Gecikme ücreti hesapla
                dt.Columns.Add("GecikmeUcreti", typeof(decimal));
                foreach (DataRow row in dt.Rows)
                {
                    var gecikmeGun = Convert.ToInt32(row["GecikmeGun"]);
                    row["GecikmeUcreti"] = gecikmeGun > 0 ? gecikmeGun * gecikmeUcreti : 0;
                }
                
                dgOdunc.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void YeniOdunc_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OduncDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadOduncler();
                LoadStats();
            }
        }
        
        private void IadeAl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                if (row["IadeTarihi"] != DBNull.Value)
                {
                    MessageBox.Show("Bu kitap zaten iade edilmiş!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var gecikmeGun = Convert.ToInt32(row["GecikmeGun"]);
                var gecikmeUcreti = Convert.ToDecimal(row["GecikmeUcreti"]);
                
                var mesaj = gecikmeGun > 0 
                    ? $"Bu kitabı iade almak istiyor musunuz?\n\n⚠️ {gecikmeGun} gün gecikme!\n💰 Gecikme ücreti: ₺{gecikmeUcreti:F2}"
                    : "Bu kitabı iade almak istiyor musunuz?";
                
                if (MessageBox.Show(mesaj, "İade Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var conn = DatabaseHelper.GetConnection();
                        conn.Open();
                        
                        using var cmd = new SqlCommand(@"
                            UPDATE OduncIslemleri SET IadeTarihi = GETDATE(), CezaMiktari = @ceza WHERE IslemID = @id;
                            UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 
                            WHERE KitapID = (SELECT KitapID FROM OduncIslemleri WHERE IslemID = @id)", conn);
                        cmd.Parameters.AddWithValue("@id", row["IslemID"]);
                        cmd.Parameters.AddWithValue("@ceza", gecikmeUcreti);
                        cmd.ExecuteNonQuery();
                        
                        LoadOduncler();
                        LoadStats();
                        
                        var iadeMesaj = gecikmeGun > 0
                            ? $"✅ Kitap iade alındı!\n\n💰 Tahsil edilecek: ₺{gecikmeUcreti:F2}"
                            : "✅ Kitap başarıyla iade alındı!";
                        
                        MessageBox.Show(iadeMesaj, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"İade işlemi başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) LoadOduncler();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoaded) LoadOduncler();
        }

        private void BarkodTara_Click(object sender, RoutedEventArgs e)
        {
            var scanner = new BarcodeScannerDialog();
            if (scanner.ShowDialog() == true)
            {
                txtSearch.Text = scanner.ScannedBarcode;
            }
        }
    }
}


