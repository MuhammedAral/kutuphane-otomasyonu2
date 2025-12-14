using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class ProfilPage : Page
    {
        private readonly int _userId;

        public ProfilPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            Loaded += async (s, e) => await LoadProfilAsync();
        }
        
        private async Task LoadProfilAsync()
        {
            try
            {
                var profil = await ApiService.GetUyeProfilAsync(_userId);
                if (profil != null)
                {
                    txtAdSoyad.Text = profil.AdSoyad;
                    txtUsername.Text = profil.KullaniciAdi;
                    txtEmail.Text = profil.Email;
                    txtTelefon.Text = profil.Telefon;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bilgiler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Kaydet_Click(object sender, RoutedEventArgs e)
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
                var request = new ProfilUpdateRequest
                {
                    AdSoyad = adSoyad,
                    KullaniciAdi = username,
                    Email = string.IsNullOrEmpty(email) ? null : email,
                    Telefon = string.IsNullOrEmpty(telefon) ? null : telefon,
                    MevcutSifre = currentPass,
                    YeniSifre = string.IsNullOrEmpty(newPass) ? null : newPass
                };
                
                var result = await ApiService.UpdateUyeProfilAsync(_userId, request);
                
                if (result != null && result.Success)
                {
                    MessageBox.Show(result.Mesaj ?? "Profil bilgileriniz başarıyla güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtCurrentPassword.Clear();
                    txtNewPassword.Clear();
                    txtNewPasswordConfirm.Clear();
                }
                else
                {
                    MessageBox.Show(result?.Mesaj ?? "Güncelleme başarısız!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncelleme başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
