using Microsoft.Data.SqlClient;
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
            LoadStats();
            LoadSon();
        }
        
        private void LoadStats()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            // Ödünçte
            using var cmd1 = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE UyeID = @id AND Durum = 'Odunc'", conn);
            cmd1.Parameters.AddWithValue("@id", _userId);
            txtOdunc.Text = cmd1.ExecuteScalar()?.ToString() ?? "0";
            
            // Geciken
            using var cmd2 = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE UyeID = @id AND Durum = 'Odunc' AND BeklenenIadeTarihi < GETDATE()", conn);
            cmd2.Parameters.AddWithValue("@id", _userId);
            txtGeciken.Text = cmd2.ExecuteScalar()?.ToString() ?? "0";
            
            // Toplam
            using var cmd3 = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE UyeID = @id", conn);
            cmd3.Parameters.AddWithValue("@id", _userId);
            txtToplam.Text = cmd3.ExecuteScalar()?.ToString() ?? "0";
        }
        
        private void LoadSon()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            using var cmd = new SqlCommand(@"
                SELECT TOP 5 k.Baslik, o.OduncTarihi, o.BeklenenIadeTarihi, 
                    CASE WHEN o.Durum = 'Odunc' THEN '📖 Ödünçte' ELSE '✅ İade Edildi' END as DurumText
                FROM OduncIslemleri o
                JOIN Kitaplar k ON o.KitapID = k.KitapID
                WHERE o.UyeID = @id
                ORDER BY o.IslemID DESC", conn);
            cmd.Parameters.AddWithValue("@id", _userId);
            
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            dgSon.ItemsSource = dt.DefaultView;
        }
    }
}
