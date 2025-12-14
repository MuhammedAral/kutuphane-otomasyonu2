using System.Data;
using System.Windows;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitapDialog : Window
    {
        private readonly int? _kitapId;
        
        public KitapDialog(int? kitapId = null)
        {
            InitializeComponent();
            Loaded += async (s, e) => 
            {
                DarkModeHelper.EnableDarkMode(this);
                await LoadTurlerAsync();
                if (_kitapId.HasValue)
                {
                    await LoadKitapAsync();
                }
            };
            _kitapId = kitapId;
            
            if (_kitapId.HasValue)
            {
                txtTitle.Text = "📖 Kitap Düzenle";
            }
        }
        
        private async Task LoadTurlerAsync()
        {
            try
            {
                var turler = await ApiService.GetTurlerAsync();
                if (turler != null)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("TurID", typeof(int));
                    dt.Columns.Add("TurAdi", typeof(string));
                    foreach (var tur in turler)
                    {
                        dt.Rows.Add(tur.TurID, tur.TurAdi);
                    }
                    cmbTur.ItemsSource = dt.DefaultView;
                }
            }
            catch { }
        }
        
        private async Task LoadKitapAsync()
        {
            try
            {
                var kitap = await ApiService.GetKitapAsync(_kitapId!.Value);
                if (kitap != null)
                {
                    txtBaslik.Text = kitap.Baslik;
                    txtYazar.Text = kitap.Yazar;
                    txtYil.Text = kitap.YayinYili?.ToString();
                    txtStok.Text = kitap.StokAdedi.ToString();
                    txtBarkod.Text = kitap.ISBN;
                    txtRaf.Text = kitap.RafNo;
                    txtSira.Text = kitap.SiraNo;
                    txtAciklama.Text = kitap.Aciklama;
                    cmbTur.SelectedValue = kitap.TurID;
                }
            }
            catch { }
        }
        
        private async void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaslik.Text) || string.IsNullOrWhiteSpace(txtYazar.Text))
            {
                MessageBox.Show("Lütfen zorunlu alanları doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string barkod = txtBarkod.Text.Trim();
            
            // Barkod zorunlu kontrol
            if (string.IsNullOrEmpty(barkod))
            {
                MessageBox.Show("Barkod alanı zorunludur!\n\nBarkod tarayıcı ile tarama yapabilir veya 13 haneli barkod numarasını manuel girebilirsiniz.", 
                    "Barkod Gerekli", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBarkod.Focus();
                return;
            }
            
            if (barkod.Length != 13)
            {
                MessageBox.Show("Barkod 13 haneli olmalıdır!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBarkod.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(txtYil.Text))
            {
                if (txtYil.Text.Length != 4 || !int.TryParse(txtYil.Text, out int yil))
                {
                    MessageBox.Show("Yayın yılı 4 haneli bir sayı olmalıdır (Örn: 2023)!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (yil < 1000 || yil > DateTime.Now.Year + 1)
                {
                    MessageBox.Show($"Yayın yılı 1000 ile {DateTime.Now.Year + 1} arasında olmalıdır!", "Geçersiz Tarih", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            
            try
            {
                var request = new KitapRequest
                {
                    Baslik = txtBaslik.Text.Trim(),
                    Yazar = txtYazar.Text.Trim(),
                    ISBN = barkod,
                    YayinYili = int.TryParse(txtYil.Text, out var yil2) ? yil2 : null,
                    TurID = cmbTur.SelectedValue as int?,
                    StokAdedi = int.TryParse(txtStok.Text, out var stok) ? stok : 1,
                    RafNo = string.IsNullOrEmpty(txtRaf.Text) ? null : txtRaf.Text,
                    SiraNo = string.IsNullOrEmpty(txtSira.Text) ? null : txtSira.Text,
                    Aciklama = string.IsNullOrEmpty(txtAciklama.Text) ? null : txtAciklama.Text
                };
                
                ApiResponse? result;
                if (_kitapId.HasValue)
                {
                    result = await ApiService.UpdateKitapAsync(_kitapId.Value, request);
                }
                else
                {
                    result = await ApiService.CreateKitapAsync(request);
                }
                
                if (result != null && result.Success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(result?.Mesaj ?? "Kayıt yapılamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt yapılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();
        private void BarkodTara_Click(object sender, RoutedEventArgs e)
        {
            var scanner = new BarcodeScannerDialog();
            if (scanner.ShowDialog() == true)
            {
                txtBarkod.Text = scanner.ScannedBarcode;
            }
        }
    }
}
