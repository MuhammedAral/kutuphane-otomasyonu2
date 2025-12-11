using Microsoft.Data.SqlClient;
using System.Windows;

namespace KutuphaneOtomasyon.Views
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
        }
        
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (password.Length < 6)
            {
                MessageBox.Show("Şifre en az 6 karakter olmalıdır!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
                checkCmd.Parameters.AddWithValue("@user", username);
                if ((int)checkCmd.ExecuteScalar() == 0)
                {
                    MessageBox.Show("Kullanıcı bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var hash = DatabaseHelper.HashPassword(password);
                using var updateCmd = new SqlCommand("UPDATE Kullanicilar SET Sifre = @pass WHERE KullaniciAdi = @user", conn);
                updateCmd.Parameters.AddWithValue("@pass", hash);
                updateCmd.Parameters.AddWithValue("@user", username);
                updateCmd.ExecuteNonQuery();
                
                MessageBox.Show("Şifreniz başarıyla değiştirildi!\n\nYeni şifrenizle giriş yapabilirsiniz.", 
                    "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifre sıfırlanamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
