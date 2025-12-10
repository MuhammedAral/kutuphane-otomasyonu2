using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class OduncDialog : Window
    {
        private DataTable _kitaplar = new();
        private DataTable _uyeler = new();
        private int? _selectedKitapId;
        private int? _selectedUyeId;
        
        public OduncDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkModeHelper.EnableDarkMode(this);
            LoadData();
            txtGun.Text = DatabaseHelper.GetMaxOduncGun().ToString();
        }
        
        private void LoadData()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            // Mevcut kitaplar - Kitap adı ve yazar
            var kitapAdapter = new SqlDataAdapter("SELECT KitapID, Baslik + ' - ' + Yazar as Baslik FROM Kitaplar WHERE MevcutAdet > 0 ORDER BY Baslik", conn);
            kitapAdapter.Fill(_kitaplar);
            lstKitaplar.ItemsSource = _kitaplar.DefaultView;
            
            // Üyeler
            var uyeAdapter = new SqlDataAdapter("SELECT KullaniciID, AdSoyad FROM Kullanicilar WHERE Rol = 'Uye' ORDER BY AdSoyad", conn);
            uyeAdapter.Fill(_uyeler);
            lstUyeler.ItemsSource = _uyeler.DefaultView;
        }
        
        private void KitapAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = txtKitapAra.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(search))
                _kitaplar.DefaultView.RowFilter = "";
            else
                _kitaplar.DefaultView.RowFilter = $"Baslik LIKE '%{search}%'";
        }
        
        private void UyeAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = txtUyeAra.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(search))
                _uyeler.DefaultView.RowFilter = "";
            else
                _uyeler.DefaultView.RowFilter = $"AdSoyad LIKE '%{search}%'";
        }
        
        private void Kitap_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (lstKitaplar.SelectedItem is DataRowView row)
            {
                _selectedKitapId = Convert.ToInt32(row["KitapID"]);
                txtKitapAra.Text = row["Baslik"].ToString();
            }
        }
        
        private void Uye_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (lstUyeler.SelectedItem is DataRowView row)
            {
                _selectedUyeId = Convert.ToInt32(row["KullaniciID"]);
                txtUyeAra.Text = row["AdSoyad"].ToString();
            }
        }
        
        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKitapId == null || _selectedUyeId == null)
            {
                MessageBox.Show("Lütfen listeden kitap ve üye seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var maxGun = DatabaseHelper.GetMaxOduncGun();
            if (!int.TryParse(txtGun.Text, out var gun) || gun <= 0) gun = maxGun;
            
            if (gun > maxGun)
            {
                MessageBox.Show($"Maksimum ödünç süresi {maxGun} gündür!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGun.Text = maxGun.ToString();
                return;
            }
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var cmd = new SqlCommand(@"
                    INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi) 
                    VALUES (@kitap, @uye, DATEADD(DAY, @gun, GETDATE()));
                    UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = @kitap", conn);
                cmd.Parameters.AddWithValue("@kitap", _selectedKitapId);
                cmd.Parameters.AddWithValue("@uye", _selectedUyeId);
                cmd.Parameters.AddWithValue("@gun", gun);
                cmd.ExecuteNonQuery();
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ödünç verilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();
    }
}
