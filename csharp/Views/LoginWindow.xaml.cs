using System.Windows;
using System.Windows.Input;

namespace KutuphaneOtomasyon.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
            txtUsername.Focus();
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login_Click(sender, e);
            }
        }
        
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifrenizi girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            btnLogin.IsEnabled = false;
            btnLogin.Content = "Giriş yapılıyor...";
            
            try
            {
                var result = DatabaseHelper.VerifyLogin(username, password);
                
                if (result.Success)
                {
                    if (result.Rol == "Yonetici")
                    {
                        var adminWindow = new AdminWindow(result.UserId!.Value, result.AdSoyad!);
                        adminWindow.Show();
                    }
                    else
                    {
                        var memberWindow = new MemberWindow(result.UserId!.Value, result.AdSoyad!);
                        memberWindow.Show();
                    }
                    Close();
                }
                else
                {
                    MessageBox.Show(result.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnLogin.IsEnabled = true;
                    btnLogin.Content = "Giriş Yap";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Giriş Yap";
            }
        }
        
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }
        
        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var forgotWindow = new ForgotPasswordWindow();
            forgotWindow.ShowDialog();
        }
    }
}
