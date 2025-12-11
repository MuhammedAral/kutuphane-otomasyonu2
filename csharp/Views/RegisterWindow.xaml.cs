using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Text.Json.Nodes;
using KutuphaneOtomasyon;

namespace KutuphaneOtomasyon.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:5026/") };
        private int _newUserId = 0;

        public RegisterWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
        }
        
        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            var adSoyad = txtAdSoyad.Text.Trim();
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;
            var email = txtEmail.Text.Trim();
            var telefon = txtTelefon.Text.Trim();
            
            if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Lütfen zorunlu alanları (Email dahil) doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!email.EndsWith("@gmail.com"))
            {
                MessageBox.Show("Güvenlik gereği sadece @gmail.com uzantılı mail adresleri kabul edilmektedir.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            btnRegister.IsEnabled = false;
            btnRegister.Content = "Kaydediliyor...";
            
            try
            {
                var payload = new { KullaniciAdi = username, Sifre = password, AdSoyad = adSoyad, Email = email, Telefon = telefon };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _client.PostAsync("api/auth/register", content);
                var resultJson = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonObject>(resultJson);
                    _newUserId = (int)result!["userId"]!;
                    
                    MessageBox.Show("Kayıt başarılı! Lütfen e-mail adresinize gelen doğrulama kodunu giriniz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Arayüzü Değiştir
                    pnlForm.Visibility = Visibility.Collapsed;
                    pnlVerify.Visibility = Visibility.Visible;
                    txtVerifyCode.Focus();
                }
                else
                {
                    string message = resultJson;
                    if (string.IsNullOrWhiteSpace(message)) message = "Bilinmeyen bir hata oluştu (Boş cevap).";

                    try {
                        var errorObj = JsonSerializer.Deserialize<JsonObject>(resultJson);
                        if (errorObj != null && errorObj.ContainsKey("message"))
                            message = errorObj["message"]!.ToString();
                    } catch { }
                    
                    MessageBox.Show("Hata Detayı:\n" + message, "Kayıt Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnRegister.IsEnabled = true;
                    btnRegister.Content = "Kayıt Ol";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRegister.IsEnabled = true;
                btnRegister.Content = "Kayıt Ol";
            }
        }

        private async void Verify_Click(object sender, RoutedEventArgs e)
        {
            var code = txtVerifyCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            btnVerify.IsEnabled = false;
            btnVerify.Content = "Doğrulanıyor...";

            try
            {
                var payload = new { UserId = _newUserId, Kod = code };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("api/auth/verify-email", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Hesabınız başarıyla doğrulandı. Artık giriş yapabilirsiniz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
                else
                {
                     string message = resultJson;
                    try {
                        var errorObj = JsonSerializer.Deserialize<JsonObject>(resultJson);
                        if (errorObj != null && errorObj.ContainsKey("message"))
                            message = errorObj["message"]!.ToString();
                    } catch {}

                    MessageBox.Show("Hata: " + message, "Doğrulama Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnVerify.IsEnabled = true;
                    btnVerify.Content = "Doğrula ve Tamamla";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
                btnVerify.IsEnabled = true;
                btnVerify.Content = "Doğrula ve Tamamla";
            }
        }
        
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
