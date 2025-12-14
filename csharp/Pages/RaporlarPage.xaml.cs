using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class RaporlarPage : Page
    {
        public RaporlarPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadStatsAsync();
        }
        
        private async Task LoadStatsAsync()
        {
            try
            {
                var raporlar = await ApiService.GetRaporlarAsync();
                
                if (raporlar != null)
                {
                    // Temel istatistikler
                    txtToplamKitap.Text = raporlar.ToplamKitap.ToString();
                    txtToplamUye.Text = raporlar.ToplamUye.ToString();
                    txtAktifOdunc.Text = raporlar.AktifOdunc.ToString();
                    txtGeciken.Text = raporlar.Geciken.ToString();
                    txtBuAyOdunc.Text = raporlar.BuAyOdunc.ToString();
                    txtBuAyIade.Text = raporlar.BuAyIade.ToString();
                    txtToplamGecikme.Text = $"₺{raporlar.ToplamGecikmeUcreti:F0}";
                    
                    // En çok ödünç alınan kitaplar
                    icTopKitaplar.ItemsSource = raporlar.TopKitaplar;
                    
                    // En aktif üyeler
                    icTopUyeler.ItemsSource = raporlar.TopUyeler;
                }
            }
            catch (Exception)
            {
                // Raporlar yüklenemezse sayfa boş kalır, kritik hata değil
            }
        }
    }
}
