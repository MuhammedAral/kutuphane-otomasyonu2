using System.Data;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class AnasayfaPage : Page
    {
        private readonly int _userId;
        
        public AnasayfaPage(int userId, string adSoyad)
        {
            InitializeComponent();
            _userId = userId;
            txtWelcome.Text = $"Hoş Geldin, {adSoyad}! 👋";
            Loaded += async (s, e) => await LoadDataAsync();
        }
        
        private async Task LoadDataAsync()
        {
            await Task.WhenAll(LoadStatsAsync(), LoadSonAsync());
        }
        
        private async Task LoadStatsAsync()
        {
            try
            {
                var stats = await ApiService.GetUyeStatsAsync(_userId);
                if (stats != null)
                {
                    txtOdunc.Text = stats.Oduncte.ToString();
                    txtGeciken.Text = stats.Geciken.ToString();
                    txtToplam.Text = stats.Toplam.ToString();
                }
            }
            catch { }
        }
        
        private async Task LoadSonAsync()
        {
            try
            {
                var islemler = await ApiService.GetUyeSonIslemlerAsync(_userId);
                if (islemler != null)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Baslik", typeof(string));
                    dt.Columns.Add("OduncTarihi", typeof(DateTime));
                    dt.Columns.Add("BeklenenIadeTarihi", typeof(DateTime));
                    dt.Columns.Add("DurumText", typeof(string));
                    
                    foreach (var i in islemler)
                    {
                        var durumText = i.Durum == "Ödünçte" ? "📖 Ödünçte" : "✅ İade Edildi";
                        dt.Rows.Add(i.Baslik, i.OduncTarihi, i.BeklenenIadeTarihi, durumText);
                    }
                    
                    dgSon.ItemsSource = dt.DefaultView;
                }
            }
            catch { }
        }
    }
}
