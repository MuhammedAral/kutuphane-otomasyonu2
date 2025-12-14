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
        
        private async void Kaydet_Click(object sender, RoutedEventArgs e)
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
                var uye = new UyeRequest
                {
                    KullaniciAdi = txtUsername.Text.Trim(),
                    Sifre = txtPassword.Password,
                    AdSoyad = txtAdSoyad.Text.Trim(),
                    Email = string.IsNullOrEmpty(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    Telefon = string.IsNullOrEmpty(txtTelefon.Text) ? null : txtTelefon.Text.Trim()
                };
                
                var result = await ApiService.CreateUyeAsync(uye);
                
                if (result != null && result.Success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(result?.Mesaj ?? result?.Message ?? "Kayıt yapılamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt yapılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();
    }
}
