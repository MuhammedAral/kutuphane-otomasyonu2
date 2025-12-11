using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;

namespace KutuphaneOtomasyon.Pages
{
    public partial class UyelerPage : Page
    {
        public UyelerPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadUyeler();
        }
        
        private void LoadUyeler(string search = "")
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var query = @"SELECT KullaniciID, KullaniciAdi, AdSoyad, 
                              ISNULL(Email, '-') as Email, ISNULL(Telefon, '-') as Telefon, Rol 
                              FROM Kullanicilar WHERE 1=1";
                
                // Arama filtresi
                if (!string.IsNullOrEmpty(search))
                    query += " AND (AdSoyad LIKE @search OR KullaniciAdi LIKE @search OR Email LIKE @search)";
                
                // Rol filtresi
                if (cmbRol?.SelectedIndex == 1) // Üyeler
                    query += " AND Rol = 'Uye'";
                else if (cmbRol?.SelectedIndex == 2) // Yöneticiler
                    query += " AND Rol = 'Yonetici'";
                
                query += " ORDER BY KullaniciID DESC";
                
                var cmd = new SqlCommand(query, conn);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                
                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgUyeler.ItemsSource = dt.DefaultView;
                
                txtSonuc.Text = $"{dt.Rows.Count} üye bulundu";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Üyeler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadUyeler(txtSearch?.Text?.Trim() ?? "");
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Özel sıralama mantığı
            if (e.Column.Header.ToString() == "Ad Soyad")
            {
                e.Handled = true; // Varsayılan sıralamayı iptal et
                
                var view = CollectionViewSource.GetDefaultView(dgUyeler.ItemsSource);
                if (view == null) return;

                var direction = ListSortDirection.Ascending;
                
                // Mevcut sıralama yönünü kontrol et ve tersine çevir
                if (e.Column.SortDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                
                // Sıralama yönünü güncelle (UI için)
                e.Column.SortDirection = direction;
                
                // Sıralamayı uygula: Önce Ad Soyad, Sonra Kullanıcı Adı
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("AdSoyad", direction));
                view.SortDescriptions.Add(new SortDescription("KullaniciAdi", ListSortDirection.Ascending)); 
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
                        using var cmd = new SqlCommand("DELETE FROM Kullanicilar WHERE KullaniciID = @id", conn);
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

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => LoadUyeler(txtSearch.Text.Trim());
    }
}
