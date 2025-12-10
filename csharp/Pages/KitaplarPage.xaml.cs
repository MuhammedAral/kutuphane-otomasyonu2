using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitaplarPage : Page
    {
        public KitaplarPage()
        {
            InitializeComponent();
            LoadKitaplar();
        }
        
        private void LoadKitaplar(string search = "")
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var query = @"
                    SELECT k.KitapID, k.Baslik, k.Yazar, ISNULL(k.ISBN, '') as ISBN, 
                           ISNULL(kt.TurAdi, '-') as TurAdi, k.StokAdedi, k.MevcutAdet
                    FROM Kitaplar k
                    LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID";
                
                if (!string.IsNullOrEmpty(search))
                    query += " WHERE k.Baslik LIKE @search OR k.Yazar LIKE @search";
                
                query += " ORDER BY k.KitapID DESC";
                
                var cmd = new SqlCommand(query, conn);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                
                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgKitaplar.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kitaplar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void YeniKitap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new KitapDialog();
                if (dialog.ShowDialog() == true)
                    LoadKitaplar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private DataRowView? GetRowFromButton(object sender)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
                return row;
            return null;
        }
        
        private void Duzenle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = GetRowFromButton(sender);
                if (row == null) return;
                
                var dialog = new KitapDialog((int)row["KitapID"]);
                if (dialog.ShowDialog() == true)
                    LoadKitaplar();
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
                var row = GetRowFromButton(sender);
                if (row == null) return;
                
                if (MessageBox.Show($"'{row["Baslik"]}' kitabını silmek istiyor musunuz?", "Silme Onayı",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM Kitaplar WHERE KitapID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", row["KitapID"]);
                    cmd.ExecuteNonQuery();
                    LoadKitaplar();
                    MessageBox.Show("Kitap silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silinemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Search_Click(object sender, RoutedEventArgs e) => LoadKitaplar(txtSearch.Text.Trim());
        private void Search_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadKitaplar(txtSearch.Text.Trim()); }
    }
}
