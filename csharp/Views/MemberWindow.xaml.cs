using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Views
{
    public partial class MemberWindow : Window
    {
        private readonly int _userId;
        private readonly string _adSoyad;
        private Button? _activeButton;
        
        public MemberWindow(int userId, string adSoyad)
        {
            InitializeComponent();
            _userId = userId;
            _adSoyad = adSoyad;
            txtUserName.Text = $"👤 {adSoyad}";
            
            Anasayfa_Click(btnAnasayfa, null!);
        }
        
        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
                _activeButton.Background = System.Windows.Media.Brushes.Transparent;
            
            button.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3b82f6"));
            _activeButton = button;
        }
        
        private void Anasayfa_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new MemberPages.AnasayfaPage(_userId, _adSoyad));
        }
        
        private void Kitaplar_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new MemberPages.KitaplarViewPage());
        }
        
        private void Odunclerim_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new MemberPages.OdunclerimPage(_userId));
        }
        
        private void Profil_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainFrame.Navigate(new MemberPages.ProfilPage(_userId));
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
