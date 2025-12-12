using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await Task.WhenAll(LoadStatsAsync(), LoadOverdueBooksAsync());
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                // API'den istatistikleri al
                var stats = await ApiService.GetDashboardStatsAsync();
                
                if (stats != null)
                {
                    txtTotalBooks.Text = stats.ToplamKitap.ToString();
                    txtTotalMembers.Text = stats.ToplamUye.ToString();
                    txtActiveLoans.Text = stats.AktifOdunc.ToString();
                    txtOverdueBooks.Text = stats.Gecikenler.ToString();
                }
                else
                {
                    MessageBox.Show("İstatistikler yüklenemedi. API çalışıyor mu?", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstatistikler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadOverdueBooksAsync()
        {
            try
            {
                // API'den geciken kitapları al
                var gecikenler = await ApiService.GetGecikenKitaplarAsync();
                
                if (gecikenler != null && gecikenler.Count > 0)
                {
                    dgOverdue.ItemsSource = gecikenler;
                    dgOverdue.Visibility = Visibility.Visible;
                    txtNoOverdue.Visibility = Visibility.Collapsed;
                }
                else
                {
                    dgOverdue.Visibility = Visibility.Collapsed;
                    txtNoOverdue.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gecikme listesi yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuickAddBook_Click(object sender, RoutedEventArgs e) => OpenAddBookDialog();
        
        private void QuickAddBook_Border_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenAddBookDialog();
        
        private async void OpenAddBookDialog()
        {
            var dialog = new KitapDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadStatsAsync();
            }
        }

        private void QuickAddMember_Click(object sender, RoutedEventArgs e) => OpenAddMemberDialog();
        
        private void QuickAddMember_Border_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenAddMemberDialog();
        
        private async void OpenAddMemberDialog()
        {
            var dialog = new UyeDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadStatsAsync();
            }
        }

        private void QuickLoan_Click(object sender, RoutedEventArgs e) => OpenLoanDialog();
        
        private void QuickLoan_Border_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenLoanDialog();
        
        private async void OpenLoanDialog()
        {
            var dialog = new OduncDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }
    }
}
