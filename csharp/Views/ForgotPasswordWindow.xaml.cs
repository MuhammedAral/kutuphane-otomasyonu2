using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace KutuphaneOtomasyon.Views
{
    public partial class ForgotPasswordWindow : Window
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:5026/") };
        
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private async void btnKodGonder_Click(object sender, RoutedEventArgs e)
        {
            var email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Lütfen e-posta adresinizi girin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnKodGonder.IsEnabled = false;
            btnKodGonder.Content = "Gönderiliyor...";

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(new { Email = email }), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("api/auth/sifremi-unuttum", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Doğrulama kodu e-postanıza gönderildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    pnlEmail.Visibility = Visibility.Collapsed;
                    pnlReset.Visibility = Visibility.Visible;
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Hata: " + msg, "Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnKodGonder.IsEnabled = true;
                    btnKodGonder.Content = "Doğrulama Kodu Gönder";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sunucuya bağlanılamadı: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                btnKodGonder.IsEnabled = true;
                btnKodGonder.Content = "Doğrulama Kodu Gönder";
            }
        }

        private async void btnSifreSifirla_Click(object sender, RoutedEventArgs e)
        {
            var kod = txtKod.Text.Trim();
            var yeniSifre = txtYeniSifre.Password;

            if (string.IsNullOrEmpty(kod) || string.IsNullOrEmpty(yeniSifre))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSifreSifirla.IsEnabled = false;

            try
            {
                var payload = new { Kod = kod, YeniSifre = yeniSifre };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _client.PostAsync("api/auth/sifre-sifirla", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Hata: " + msg, "Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnSifreSifirla.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSifreSifirla.IsEnabled = true;
            }
        }

        private void btnIptal_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
