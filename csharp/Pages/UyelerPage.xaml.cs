using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KutuphaneOtomasyon.Pages
{
    public partial class UyelerPage : Page
    {
        public UyelerPage()
        {
            InitializeComponent();
            LoadUyeler();
        }
        
        private void LoadUyeler(string search = "")
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var query = @"SELECT KullaniciID, KullaniciAdi, AdSoyad, 
                              ISNULL(Email, '-') as Email, ISNULL(Telefon, '-') as Telefon, Rol 
                              FROM Kullanicilar";
                if (!string.IsNullOrEmpty(search))
                    query += " WHERE AdSoyad LIKE @search OR KullaniciAdi LIKE @search";
                query += " ORDER BY KullaniciID DESC";
                
                var cmd = new SqlCommand(query, conn);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                
                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgUyeler.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Üyeler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void YeniUye_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new UyeDialog();
                if (dialog.ShowDialog() == true)
                    LoadUyeler();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Sil_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is DataRowView row)
                {
                    if (row["Rol"].ToString() == "Yonetici")
                    {
                        MessageBox.Show("Yönetici kullanıcı silinemez!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    if (MessageBox.Show($"'{row["AdSoyad"]}' üyesini silmek istiyor musunuz?", "Silme Onayı",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        using var conn = DatabaseHelper.GetConnection();
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM Kullanicilar WHERE KullaniciID = @id", conn);
                        cmd.Parameters.AddWithValue("@id", row["KullaniciID"]);
                        cmd.ExecuteNonQuery();
                        LoadUyeler();
                        MessageBox.Show("Üye silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silinemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Search_Click(object sender, RoutedEventArgs e) => LoadUyeler(txtSearch.Text.Trim());
        private void Search_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadUyeler(txtSearch.Text.Trim()); }
    }
}
