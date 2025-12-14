using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

namespace KutuphaneOtomasyon.Pages
{
    public partial class UyelerPage : Page
    {
        public UyelerPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadUyelerAsync();
        }
        
        private async Task LoadUyelerAsync(string search = "")
        {
            try
            {
                string? rol = null;
                if (cmbRol?.SelectedIndex == 1)
                    rol = "Uye";
                else if (cmbRol?.SelectedIndex == 2)
                    rol = "Yonetici";
                
                var uyeler = await ApiService.GetUyelerAsync(search, rol);
                
                if (uyeler != null)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("KullaniciID", typeof(int));
                    dt.Columns.Add("KullaniciAdi", typeof(string));
                    dt.Columns.Add("AdSoyad", typeof(string));
                    dt.Columns.Add("Email", typeof(string));
                    dt.Columns.Add("Telefon", typeof(string));
                    dt.Columns.Add("Rol", typeof(string));
                    
                    foreach (var u in uyeler)
                    {
                        dt.Rows.Add(u.KullaniciID, u.KullaniciAdi, u.AdSoyad, u.Email ?? "-", u.Telefon ?? "-", u.Rol);
                    }
                    
                    dgUyeler.ItemsSource = dt.DefaultView;
                    txtSonuc.Text = $"{dt.Rows.Count} üye bulundu";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Üyeler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) await LoadUyelerAsync(txtSearch?.Text?.Trim() ?? "");
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Ad Soyad")
            {
                e.Handled = true;
                
                var view = CollectionViewSource.GetDefaultView(dgUyeler.ItemsSource);
                if (view == null) return;

                var direction = ListSortDirection.Ascending;
                
                if (e.Column.SortDirection == ListSortDirection.Ascending)
                    direction = ListSortDirection.Descending;
                
                e.Column.SortDirection = direction;
                
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("AdSoyad", direction));
                view.SortDescriptions.Add(new SortDescription("KullaniciAdi", ListSortDirection.Ascending)); 
            }
        }
        
        private async void YeniUye_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new UyeDialog();
                if (dialog.ShowDialog() == true)
                    await LoadUyelerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void Sil_Click(object sender, RoutedEventArgs e)
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
                        var id = Convert.ToInt32(row["KullaniciID"]);
                        var result = await ApiService.DeleteUyeAsync(id);
                        
                        if (result != null && result.Success)
                        {
                            await LoadUyelerAsync();
                            MessageBox.Show("Üye silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(result?.Mesaj ?? "Silme başarısız!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silinemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Search_TextChanged(object sender, TextChangedEventArgs e) => await LoadUyelerAsync(txtSearch.Text.Trim());
    }
}
