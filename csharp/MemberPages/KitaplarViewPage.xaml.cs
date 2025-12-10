using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class KitaplarViewPage : Page
    {
        public KitaplarViewPage()
        {
            InitializeComponent();
            LoadKitaplar();
        }
        
        private void LoadKitaplar(string search = "")
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            var query = @"
                SELECT k.KitapID, k.Baslik, k.Yazar, kt.TurAdi, 
                    CASE WHEN k.MevcutAdet > 0 THEN '✓ Var' ELSE '✗ Yok' END as MevcutText
                FROM Kitaplar k
                LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID";
            
            if (!string.IsNullOrEmpty(search))
                query += " WHERE k.Baslik LIKE @search OR k.Yazar LIKE @search";
            
            var cmd = new SqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            
            var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            dgKitaplar.ItemsSource = dt.DefaultView;
        }
        
        private void Search_Click(object sender, RoutedEventArgs e) => LoadKitaplar(txtSearch.Text.Trim());
        private void Search_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadKitaplar(txtSearch.Text.Trim()); }
    }
}
