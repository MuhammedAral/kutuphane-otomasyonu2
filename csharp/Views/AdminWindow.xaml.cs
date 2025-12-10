using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Views
{
    public partial class AdminWindow : Window
    {
        private readonly int _userId;
        private readonly string _adSoyad;
        private Button? _activeButton;
        
        public AdminWindow(int userId, string adSoyad)
        {
            InitializeComponent();
            _userId = userId;
            _adSoyad = adSoyad;
            txtAdminName.Text = $"👤 {adSoyad} (Yönetici)";
            
            // İlk sayfa
            Kitaplar_Click(btnKitaplar, null!);
        }
        
        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
                _activeButton.Background = System.Windows.Media.Brushes.Transparent;
            
            button.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3b82f6"));
            _activeButton = button;
        }
        
        private void Kitaplar_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new Pages.KitaplarPage());
        }
        
        private void Uyeler_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new Pages.UyelerPage());
        }
        
        private void Odunc_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new Pages.OduncPage());
        }
        
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Çıkış yapmak istiyor musunuz?", "Çıkış", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var login = new LoginWindow();
                login.Show();
                Close();
            }
        }
    }
}
