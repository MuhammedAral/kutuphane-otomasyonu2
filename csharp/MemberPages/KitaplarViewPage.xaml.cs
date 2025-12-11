using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KutuphaneOtomasyon.Pages; // KitapDetayDialog burada
using Microsoft.Data.SqlClient;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class KitaplarViewPage : Page
    {
        private DataTable _kitaplar = new();

        public KitaplarViewPage()
        {
            InitializeComponent();
            LoadKitaplar();
        }

        private void LoadKitaplar(string search = "")
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            string query = @"
                SELECT k.KitapID, k.Baslik, k.Yazar, t.TurAdi, 
                CASE WHEN k.MevcutAdet > 0 THEN CAST(k.MevcutAdet AS NVARCHAR) + ' Adet' ELSE 'Tükendi' END as MevcutText
                FROM Kitaplar k
                LEFT JOIN KitapTurleri t ON k.TurID = t.TurID";
            
            if (!string.IsNullOrEmpty(search))
            {
                query += " WHERE k.Baslik LIKE @search OR k.Yazar LIKE @search OR t.TurAdi LIKE @search";
            }
            
            query += " ORDER BY k.Baslik";
            
            using var cmd = new SqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
                
            using var adapter = new SqlDataAdapter(cmd);
            _kitaplar = new DataTable();
            adapter.Fill(_kitaplar);
            dgKitaplar.ItemsSource = _kitaplar.DefaultView;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            LoadKitaplar(txtSearch.Text.Trim());
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoadKitaplar(txtSearch.Text.Trim());
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgKitaplar.SelectedItem is DataRowView row)
            {
                int kitapId = Convert.ToInt32(row["KitapID"]);
                var detay = new KitapDetayDialog(kitapId);
                detay.ShowDialog();
            }
        }
    }
}
