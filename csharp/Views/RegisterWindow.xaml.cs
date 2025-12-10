using Microsoft.Data.SqlClient;
using System.Windows;

namespace KutuphaneOtomasyon.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
        }
        
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var adSoyad = txtAdSoyad.Text.Trim();
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;
            var email = txtEmail.Text.Trim();
            var telefon = txtTelefon.Text.Trim();
            
            if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen zorunlu alanları doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (password.Length < 6)
            {
                MessageBox.Show("Şifre en az 6 karakter olmalıdır!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(telefon) && telefon.Length != 11)
            {
                MessageBox.Show("Telefon numarası 11 haneli olmalıdır (05XXXXXXXXX)!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            btnRegister.IsEnabled = false;
            btnRegister.Content = "Kaydediliyor...";
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                // Kullanıcı adı kontrolü
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
                checkCmd.Parameters.AddWithValue("@user", username);
                if ((int)checkCmd.ExecuteScalar() > 0)
                {
                    MessageBox.Show("Bu kullanıcı adı zaten kullanılıyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnRegister.IsEnabled = true;
                    btnRegister.Content = "Kayıt Ol";
                    return;
                }
                
                var hash = DatabaseHelper.HashPassword(password);
                var insertCmd = new SqlCommand(@"
                    INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol) 
                    VALUES (@user, @pass, @ad, @email, @tel, 'Uye')", conn);
                insertCmd.Parameters.AddWithValue("@user", username);
                insertCmd.Parameters.AddWithValue("@pass", hash);
                insertCmd.Parameters.AddWithValue("@ad", adSoyad);
                insertCmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? DBNull.Value : email);
                insertCmd.Parameters.AddWithValue("@tel", string.IsNullOrEmpty(telefon) ? DBNull.Value : telefon);
                insertCmd.ExecuteNonQuery();
                
                MessageBox.Show($"Kayıt başarılı!\n\nKullanıcı adınız: {username}\n\nArtık giriş yapabilirsiniz.", 
                    "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt yapılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRegister.IsEnabled = true;
                btnRegister.Content = "Kayıt Ol";
            }
        }
        
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
