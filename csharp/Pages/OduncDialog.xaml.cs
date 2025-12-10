using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;

namespace KutuphaneOtomasyon.Pages
{
    public partial class OduncDialog : Window
    {
        public OduncDialog()
        {
            InitializeComponent();
            LoadData();
        }
        
        private void LoadData()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            // Mevcut kitaplar
            var kitapAdapter = new SqlDataAdapter("SELECT KitapID, Baslik FROM Kitaplar WHERE MevcutAdet > 0", conn);
            var kitapDt = new DataTable();
            kitapAdapter.Fill(kitapDt);
            cmbKitap.ItemsSource = kitapDt.DefaultView;
            
            // Üyeler
            var uyeAdapter = new SqlDataAdapter("SELECT KullaniciID, AdSoyad FROM Kullanicilar WHERE Rol = 'Uye'", conn);
            var uyeDt = new DataTable();
            uyeAdapter.Fill(uyeDt);
            cmbUye.ItemsSource = uyeDt.DefaultView;
        }
        
        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (cmbKitap.SelectedValue == null || cmbUye.SelectedValue == null)
            {
                MessageBox.Show("Lütfen kitap ve üye seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (!int.TryParse(txtGun.Text, out var gun) || gun <= 0) gun = 14;
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var cmd = new SqlCommand(@"
                    INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi) 
                    VALUES (@kitap, @uye, DATEADD(DAY, @gun, GETDATE()));
                    UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = @kitap", conn);
                cmd.Parameters.AddWithValue("@kitap", cmbKitap.SelectedValue);
                cmd.Parameters.AddWithValue("@uye", cmbUye.SelectedValue);
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
