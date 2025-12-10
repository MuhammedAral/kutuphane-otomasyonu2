using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class OduncPage : Page
    {
        public OduncPage()
        {
            InitializeComponent();
            LoadOdunc();
        }
        
        private void LoadOdunc(string filter = "")
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var query = @"
                    SELECT o.IslemID, k.Baslik, u.AdSoyad, o.OduncTarihi, o.BeklenenIadeTarihi, o.Durum
                    FROM OduncIslemleri o
                    JOIN Kitaplar k ON o.KitapID = k.KitapID
                    JOIN Kullanicilar u ON o.UyeID = u.KullaniciID";
                
                if (filter == "Odunc")
                    query += " WHERE o.Durum = 'Odunc'";
                else if (filter == "IadeEdildi")
                    query += " WHERE o.Durum = 'IadeEdildi'";
                
                query += " ORDER BY o.IslemID DESC";
                
                var adapter = new SqlDataAdapter(query, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgOdunc.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (rbOdunc?.IsChecked == true) LoadOdunc("Odunc");
            else if (rbIade?.IsChecked == true) LoadOdunc("IadeEdildi");
            else LoadOdunc();
        }
        
        private void YeniOdunc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OduncDialog();
                if (dialog.ShowDialog() == true)
                    LoadOdunc();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void IadeAl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is DataRowView row)
                {
                    if (row["Durum"].ToString() == "IadeEdildi")
                    {
                        MessageBox.Show("Bu kitap zaten iade edilmiş!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    if (MessageBox.Show($"'{row["Baslik"]}' kitabını iade almak istiyor musunuz?", "İade Onayı",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        using var conn = DatabaseHelper.GetConnection();
                        conn.Open();
                        
                        var cmd = new SqlCommand(@"
                            UPDATE OduncIslemleri SET Durum = 'IadeEdildi', IadeTarihi = GETDATE() WHERE IslemID = @id;
                            UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 
                            WHERE KitapID = (SELECT KitapID FROM OduncIslemleri WHERE IslemID = @id)", conn);
                        cmd.Parameters.AddWithValue("@id", row["IslemID"]);
                        cmd.ExecuteNonQuery();
                        
                        LoadOdunc();
                        MessageBox.Show("Kitap iade alındı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İade alınamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
