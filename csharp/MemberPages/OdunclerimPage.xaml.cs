using Microsoft.Data.SqlClient;
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
            LoadOdunc();
        }
        
        private void LoadOdunc()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            var cmd = new SqlCommand(@"
                SELECT k.Baslik, o.OduncTarihi, o.BeklenenIadeTarihi, 
                    CASE WHEN o.Durum = 'Odunc' THEN '📖 Ödünçte' ELSE '✅ İade Edildi' END as DurumText,
                    CASE 
                        WHEN o.Durum = 'Odunc' AND o.BeklenenIadeTarihi < GETDATE() 
                        THEN CAST(DATEDIFF(DAY, o.BeklenenIadeTarihi, GETDATE()) AS VARCHAR) + ' gün'
                        ELSE '' 
                    END as Gecikme
                FROM OduncIslemleri o
                JOIN Kitaplar k ON o.KitapID = k.KitapID
                WHERE o.UyeID = @id
                ORDER BY o.IslemID DESC", conn);
            cmd.Parameters.AddWithValue("@id", _userId);
            
            var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            dgOdunc.ItemsSource = dt.DefaultView;
        }
    }
}
