using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class OduncPage : Page
    {
        private bool _isLoaded = false;
        
        public OduncPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => { _isLoaded = true; await LoadDataAsync(); };
        }
        
        private async Task LoadDataAsync()
        {
            await Task.WhenAll(LoadOdunclerAsync(), LoadStatsAsync());
        }
        
        private async Task LoadStatsAsync()
        {
            try
            {
                var stats = await ApiService.GetOduncStatsAsync();
                if (stats != null)
                {
                    txtAktifOdunc.Text = stats.AktifOdunc.ToString();
                    txtGeciken.Text = stats.GecikenOdunc.ToString();
                    txtToplamUcret.Text = $"₺{stats.ToplamUcret:F2}";
                    txtBugunIade.Text = stats.BugunIade.ToString();
                }
            }
            catch { }
        }
        
        private async Task LoadOdunclerAsync()
        {
            try
            {
                string? filter = null;
                if (rbOdunc?.IsChecked == true)
                    filter = "Oduncte";
                else if (rbIade?.IsChecked == true)
                    filter = "IadeEdilmis";
                else if (rbGeciken?.IsChecked == true)
                    filter = "Geciken";
                
                var search = txtSearch?.Text?.Trim();
                
                var oduncler = await ApiService.GetOdunclerAsync(filter, search);
                if (oduncler != null)
                {
                    var stats = await ApiService.GetOduncStatsAsync();
                    var gecikmeUcreti = stats?.GecikmeUcreti ?? 2.50m;
                    
                    var dt = new DataTable();
                    dt.Columns.Add("IslemID", typeof(int));
                    dt.Columns.Add("Baslik", typeof(string));
                    dt.Columns.Add("AdSoyad", typeof(string));
                    dt.Columns.Add("OduncTarihi", typeof(DateTime));
                    dt.Columns.Add("BeklenenIadeTarihi", typeof(DateTime));
                    dt.Columns.Add("IadeTarihi", typeof(DateTime));
                    dt.Columns.Add("Durum", typeof(string));
                    dt.Columns.Add("GecikmeGun", typeof(int));
                    dt.Columns.Add("GecikmeUcreti", typeof(decimal));
                    
                    foreach (var o in oduncler)
                    {
                        var row = dt.NewRow();
                        row["IslemID"] = o.IslemID;
                        row["Baslik"] = o.Baslik;
                        row["AdSoyad"] = o.AdSoyad;
                        row["OduncTarihi"] = o.OduncTarihi;
                        row["BeklenenIadeTarihi"] = o.BeklenenIadeTarihi;
                        if (o.IadeTarihi.HasValue) row["IadeTarihi"] = o.IadeTarihi.Value;
                        row["Durum"] = o.Durum;
                        row["GecikmeGun"] = o.GecikmeGun;
                        row["GecikmeUcreti"] = o.GecikmeGun > 0 ? o.GecikmeGun * gecikmeUcreti : 0;
                        dt.Rows.Add(row);
                    }
                    
                    dgOdunc.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void YeniOdunc_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OduncDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }
        
        private async void IadeAl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                if (row["IadeTarihi"] != DBNull.Value)
                {
                    MessageBox.Show("Bu kitap zaten iade edilmiş!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var gecikmeGun = Convert.ToInt32(row["GecikmeGun"]);
                var gecikmeUcreti = Convert.ToDecimal(row["GecikmeUcreti"]);
                
                var mesaj = gecikmeGun > 0 
                    ? $"Bu kitabı iade almak istiyor musunuz?\n\n⚠️ {gecikmeGun} gün gecikme!\n💰 Gecikme ücreti: ₺{gecikmeUcreti:F2}"
                    : "Bu kitabı iade almak istiyor musunuz?";
                
                if (MessageBox.Show(mesaj, "İade Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var islemId = Convert.ToInt32(row["IslemID"]);
                        var result = await ApiService.IadeAsync(islemId);
                        
                        if (result != null && result.Success)
                        {
                            await LoadDataAsync();
                            
                            var iadeMesaj = result.GecikmeGun > 0
                                ? $"✅ Kitap iade alındı!\n\n💰 Tahsil edilecek: ₺{result.CezaMiktari:F2}"
                                : "✅ Kitap başarıyla iade alındı!";
                            
                            MessageBox.Show(iadeMesaj, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(result?.Mesaj ?? "İade işlemi başarısız!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"İade işlemi başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private async void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) await LoadOdunclerAsync();
        }

        private async void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoaded) await LoadOdunclerAsync();
        }

        private void BarkodTara_Click(object sender, RoutedEventArgs e)
        {
            var scanner = new BarcodeScannerDialog();
            if (scanner.ShowDialog() == true)
            {
                txtSearch.Text = scanner.ScannedBarcode;
            }
        }
    }
}
