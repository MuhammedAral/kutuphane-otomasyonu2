using Microsoft.Data.SqlClient;
using System.Windows;

namespace KutuphaneOtomasyon.Pages
{
    public partial class UyeDialog : Window
    {
        public UyeDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
        }
        
        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAdSoyad.Text) || 
                string.IsNullOrWhiteSpace(txtUsername.Text) || 
                string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Lütfen zorunlu alanları doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtPassword.Password.Length < 6)
            {
                MessageBox.Show("Şifre en az 6 karakter olmalıdır!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string telefon = txtTelefon.Text.Trim();
            if (!string.IsNullOrEmpty(telefon) && telefon.Length != 11)
            {
                MessageBox.Show("Telefon numarası 11 haneli olmalıdır (05XXXXXXXXX)!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
                checkCmd.Parameters.AddWithValue("@user", txtUsername.Text.Trim());
                if ((int)checkCmd.ExecuteScalar() > 0)
                {
                    MessageBox.Show("Bu kullanıcı adı zaten kullanılıyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var hash = DatabaseHelper.HashPassword(txtPassword.Password);
                var cmd = new SqlCommand(@"INSERT INTO Kullanicilar 
                    (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol) 
                    VALUES (@user, @pass, @ad, @email, @tel, 'Uye')", conn);
                cmd.Parameters.AddWithValue("@user", txtUsername.Text.Trim());
                cmd.Parameters.AddWithValue("@pass", hash);
                cmd.Parameters.AddWithValue("@ad", txtAdSoyad.Text.Trim());
                cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(txtEmail.Text) ? DBNull.Value : txtEmail.Text);
                cmd.Parameters.AddWithValue("@tel", string.IsNullOrEmpty(txtTelefon.Text) ? DBNull.Value : txtTelefon.Text);
                cmd.ExecuteNonQuery();
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt yapılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();
    }
}
