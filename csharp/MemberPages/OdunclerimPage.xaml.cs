using System.Data;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class OdunclerimPage : Page
    {
        private readonly int _userId;
        
        public OdunclerimPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            Loaded += async (s, e) => await LoadOduncAsync();
        }
        
        private async Task LoadOduncAsync()
        {
            try
            {
                var oduncler = await ApiService.GetUyeOdunclerAsync(_userId);
                if (oduncler != null)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Baslik", typeof(string));
                    dt.Columns.Add("OduncTarihi", typeof(DateTime));
                    dt.Columns.Add("BeklenenIadeTarihi", typeof(DateTime));
                    dt.Columns.Add("DurumText", typeof(string));
                    dt.Columns.Add("Gecikme", typeof(string));
                    
                    foreach (var o in oduncler)
                    {
                        dt.Rows.Add(o.Baslik, o.OduncTarihi, o.BeklenenIadeTarihi, o.DurumText, o.Gecikme);
                    }
                    
                    dgOdunc.ItemsSource = dt.DefaultView;
                }
            }
            catch { }
        }
    }
}
