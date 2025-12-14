using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KutuphaneOtomasyon.Pages;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class KitaplarViewPage : Page
    {
        private DataTable _kitaplar = new();

        public KitaplarViewPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadKitaplarAsync();
        }

        private async Task LoadKitaplarAsync(string search = "")
        {
            try
            {
                var kitaplar = await ApiService.GetKitaplarAsync(search);
                
                _kitaplar = new DataTable();
                _kitaplar.Columns.Add("KitapID", typeof(int));
                _kitaplar.Columns.Add("Baslik", typeof(string));
                _kitaplar.Columns.Add("Yazar", typeof(string));
                _kitaplar.Columns.Add("TurAdi", typeof(string));
                _kitaplar.Columns.Add("MevcutText", typeof(string));
                
                if (kitaplar != null)
                {
                    foreach (var k in kitaplar)
                    {
                        var mevcutText = k.MevcutAdet > 0 ? $"{k.MevcutAdet} Adet" : "Tükendi";
                        _kitaplar.Rows.Add(k.KitapID, k.Baslik, k.Yazar ?? "-", k.TurAdi ?? "-", mevcutText);
                    }
                }
                
                dgKitaplar.ItemsSource = _kitaplar.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kitaplar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await LoadKitaplarAsync(txtSearch.Text.Trim());
        }

        private async void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await LoadKitaplarAsync(txtSearch.Text.Trim());
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
