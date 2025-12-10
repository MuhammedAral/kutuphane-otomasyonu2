using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class ProfilPage : Page
    {
        private readonly int _userId;

        public ProfilPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadProfil();
        }
        
        private void LoadProfil()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var cmd = new SqlCommand("SELECT AdSoyad, KullaniciAdi, Email, Telefon FROM Kullanicilar WHERE KullaniciID = @id", conn);
                cmd.Parameters.AddWithValue("@id", _userId);
                
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtAdSoyad.Text = reader["AdSoyad"]?.ToString();
                    txtUsername.Text = reader["KullaniciAdi"]?.ToString();
                    txtEmail.Text = reader["Email"]?.ToString();
                    txtTelefon.Text = reader["Telefon"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bilgiler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            var adSoyad = txtAdSoyad.Text.Trim();
            var username = txtUsername.Text.Trim();
            var email = txtEmail.Text.Trim();
            var telefon = txtTelefon.Text.Trim();
            var currentPass = txtCurrentPassword.Password;
            var newPass = txtNewPassword.Password;
            var newPassConfirm = txtNewPasswordConfirm.Password;

            if (string.IsNullOrEmpty(currentPass))
            {
                MessageBox.Show("Değişiklikleri kaydetmek için lütfen mevcut şifrenizi girin.", "Güvenlik Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Ad Soyad ve Kullanıcı Adı boş bırakılamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Telefon ve Şifre uzunluk kontrolleri (Kullanıcının önceki isteği)
            if (!string.IsNullOrEmpty(telefon) && telefon.Length != 11)
            {
                MessageBox.Show("Telefon numarası 11 haneli olmalıdır (05XXXXXXXXX)!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(newPass))
            {
                 if (newPass.Length < 6)
                 {
                     MessageBox.Show("Yeni şifre en az 6 karakter olmalıdır!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                     return;
                 }
                 if (newPass != newPassConfirm)
                 {
                     MessageBox.Show("Yeni şifreler uyuşmuyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                     return;
                 }
            }

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // 1. Önce Mevcut Şifreyi Doğrula
                // Not: DatabaseHelper içinde Login kontrolü var ama hash'i doğrudan kontrol etsek daha hızlı (Login LastLogin güncelliyor olabilir)
                // Basitlik için DatabaseHelper.HashPassword kullanıp veritabanındaki ile karşılaştıracağız.
                
                var verifyCmd = new SqlCommand("SELECT Sifre FROM Kullanicilar WHERE KullaniciID = @id", conn);
                verifyCmd.Parameters.AddWithValue("@id", _userId);
                var storedHash = verifyCmd.ExecuteScalar()?.ToString();

                if (storedHash != DatabaseHelper.HashPassword(currentPass))
                {
                    MessageBox.Show("Mevcut şifreniz hatalı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. Kullanıcı Adı Çakışma Kontrolü (Eğer değiştirdiyse)
                var userCheckCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user AND KullaniciID != @id", conn);
                userCheckCmd.Parameters.AddWithValue("@user", username);
                userCheckCmd.Parameters.AddWithValue("@id", _userId);
                
                if ((int)userCheckCmd.ExecuteScalar() > 0)
                {
                    MessageBox.Show("Bu kullanıcı adı başka biri tarafından kullanılıyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3. Güncelleme
                string updateQuery = @"
                    UPDATE Kullanicilar SET 
                        AdSoyad = @ad, 
                        KullaniciAdi = @user, 
                        Email = @email, 
                        Telefon = @tel
                        " + (!string.IsNullOrEmpty(newPass) ? ", Sifre = @pass" : "") + @"
                    WHERE KullaniciID = @id";

                var updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@ad", adSoyad);
                updateCmd.Parameters.AddWithValue("@user", username);
                updateCmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? DBNull.Value : email);
                updateCmd.Parameters.AddWithValue("@tel", string.IsNullOrEmpty(telefon) ? DBNull.Value : telefon);
                updateCmd.Parameters.AddWithValue("@id", _userId);
                
                if (!string.IsNullOrEmpty(newPass))
                {
                     updateCmd.Parameters.AddWithValue("@pass", DatabaseHelper.HashPassword(newPass));
                }

                updateCmd.ExecuteNonQuery();
                MessageBox.Show("Profil bilgileriniz başarıyla güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Formu temizle (Şifre alanlarını)
                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtNewPasswordConfirm.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncelleme başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
