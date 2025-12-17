using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class OduncDialog : Window
    {
        private DataTable _kitaplar = new();
        private DataTable _uyeler = new();
        private int? _selectedKitapId;
        private int? _selectedUyeId;
        
        public OduncDialog()
        {
            InitializeComponent();
            Loaded += async (s, e) => {
                DarkModeHelper.EnableDarkMode(this);
                await LoadDataAsync();
            };
            txtGun.Text = DatabaseHelper.GetMaxOduncGun().ToString();
        }
        
        private async Task LoadDataAsync()
        {
            try
            {
                // API'den kitapları yükle
                var kitaplar = await ApiService.GetMevcutKitaplarAsync();
                if (kitaplar != null)
                {
                    _kitaplar = new DataTable();
                    _kitaplar.Columns.Add("kitapid", typeof(int));
                    _kitaplar.Columns.Add("baslik", typeof(string));
                    
                    foreach (var k in kitaplar)
                    {
                        var displayText = !string.IsNullOrEmpty(k.Yazar) 
                            ? $"{k.Baslik} - {k.Yazar}" 
                            : k.Baslik;
                        _kitaplar.Rows.Add(k.KitapID, displayText);
                    }
                    lstKitaplar.ItemsSource = _kitaplar.DefaultView;
                }
                
                // API'den üyeleri yükle
                var uyeler = await ApiService.GetUyelerForOduncAsync();
                if (uyeler != null)
                {
                    _uyeler = new DataTable();
                    _uyeler.Columns.Add("kullaniciid", typeof(int));
                    _uyeler.Columns.Add("adsoyad", typeof(string));
                    
                    foreach (var u in uyeler)
                    {
                        _uyeler.Rows.Add(u.KullaniciID, u.AdSoyad);
                    }
                    lstUyeler.ItemsSource = _uyeler.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void KitapAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = txtKitapAra.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(search))
                _kitaplar.DefaultView.RowFilter = "";
            else
                _kitaplar.DefaultView.RowFilter = $"baslik LIKE '%{search}%'";
        }
        
        private void UyeAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = txtUyeAra.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(search))
                _uyeler.DefaultView.RowFilter = "";
            else
                _uyeler.DefaultView.RowFilter = $"adsoyad LIKE '%{search}%'";
        }
        
        private void Kitap_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (lstKitaplar.SelectedItem is DataRowView row)
            {
                _selectedKitapId = Convert.ToInt32(row["kitapid"]);
                txtKitapAra.Text = row["baslik"].ToString();
            }
        }
        
        private void Uye_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (lstUyeler.SelectedItem is DataRowView row)
            {
                _selectedUyeId = Convert.ToInt32(row["kullaniciid"]);
                txtUyeAra.Text = row["adsoyad"].ToString();
            }
        }
        
        private async void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKitapId == null || _selectedUyeId == null)
            {
                MessageBox.Show("Lütfen listeden kitap ve üye seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var maxGun = DatabaseHelper.GetMaxOduncGun();
            if (!int.TryParse(txtGun.Text, out var gun) || gun <= 0) gun = maxGun;
            
            if (gun > maxGun)
            {
                MessageBox.Show($"Maksimum ödünç süresi {maxGun} gündür!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGun.Text = maxGun.ToString();
                return;
            }
            
            // Double-click koruması
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            
            try
            {
                var result = await ApiService.CreateOduncAsync(_selectedKitapId.Value, _selectedUyeId.Value, gun);
                if (result != null && result.Success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(result?.Mesaj ?? "Ödünç oluşturulamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (btn != null) btn.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ödünç verilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                if (btn != null) btn.IsEnabled = true;
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();

        private async void BarkodTara_Click(object sender, RoutedEventArgs e)
        {
            var scanner = new BarcodeScannerDialog();
            if (scanner.ShowDialog() == true)
            {
                var scannedIsbn = scanner.ScannedBarcode;
                try 
                {
                    // API'den ISBN kontrolü
                    var kitaplar = await ApiService.GetKitaplarAsync(scannedIsbn);
                    if (kitaplar != null && kitaplar.Count > 0)
                    {
                        var kitap = kitaplar.First();
                        txtKitapAra.Text = kitap.Baslik;
                        _selectedKitapId = kitap.KitapID;
                    }
                    else
                    {
                        MessageBox.Show("Bu barkoda sahip kitap bulunamadı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
