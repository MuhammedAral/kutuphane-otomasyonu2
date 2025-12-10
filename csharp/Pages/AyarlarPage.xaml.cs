using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class AyarlarPage : Page
    {
        public AyarlarPage()
        {
            InitializeComponent();
            LoadAyarlar();
        }
        
        private void LoadAyarlar()
        {
            try
            {
                txtMaxGun.Text = DatabaseHelper.GetMaxOduncGun().ToString();
                txtGecikmeUcreti.Text = DatabaseHelper.GetGecikmeUcreti().ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            // Validasyon
            if (!int.TryParse(txtMaxGun.Text, out var maxGun) || maxGun <= 0)
            {
                MessageBox.Show("Lütfen geçerli bir gün sayısı girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var gecikmeText = txtGecikmeUcreti.Text.Replace(",", ".");
            if (!decimal.TryParse(gecikmeText, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var gecikmeUcreti) || gecikmeUcreti < 0)
            {
                MessageBox.Show("Lütfen geçerli bir ücret girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                DatabaseHelper.SetAyar("MaxOduncGun", maxGun.ToString());
                DatabaseHelper.SetAyar("GecikmeUcreti", gecikmeUcreti.ToString(System.Globalization.CultureInfo.InvariantCulture));
                
                txtSonKayit.Text = $"✅ Ayarlar başarıyla kaydedildi - {DateTime.Now:HH:mm:ss}";
                MessageBox.Show("Ayarlar başarıyla kaydedildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
