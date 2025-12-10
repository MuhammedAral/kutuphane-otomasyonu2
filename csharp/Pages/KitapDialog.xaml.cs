using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitapDialog : Window
    {
        private readonly int? _kitapId;
        
        public KitapDialog(int? kitapId = null)
        {
            InitializeComponent();
            _kitapId = kitapId;
            LoadTurler();
            
            if (_kitapId.HasValue)
            {
                txtTitle.Text = "📚 Kitap Düzenle";
                LoadKitap();
            }
        }
        
        private void LoadTurler()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var adapter = new SqlDataAdapter("SELECT TurID, TurAdi FROM KitapTurleri", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            cmbTur.ItemsSource = dt.DefaultView;
        }
        
        private void LoadKitap()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Kitaplar WHERE KitapID = @id", conn);
            cmd.Parameters.AddWithValue("@id", _kitapId);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                txtBaslik.Text = reader["Baslik"].ToString();
                txtYazar.Text = reader["Yazar"].ToString();
                txtISBN.Text = reader["ISBN"]?.ToString();
                txtYil.Text = reader["YayinYili"]?.ToString();
                txtStok.Text = reader["StokAdedi"].ToString();
                txtRaf.Text = reader["RafNo"]?.ToString();
                cmbTur.SelectedValue = reader["TurID"];
            }
        }
        
        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaslik.Text) || string.IsNullOrWhiteSpace(txtYazar.Text))
            {
                MessageBox.Show("Lütfen zorunlu alanları doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                SqlCommand cmd;
                if (_kitapId.HasValue)
                {
                    cmd = new SqlCommand(@"UPDATE Kitaplar SET 
                        Baslik = @baslik, Yazar = @yazar, ISBN = @isbn, YayinYili = @yil, 
                        TurID = @tur, StokAdedi = @stok, MevcutAdet = @stok, RafNo = @raf
                        WHERE KitapID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", _kitapId);
                }
                else
                {
                    cmd = new SqlCommand(@"INSERT INTO Kitaplar 
                        (Baslik, Yazar, ISBN, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo)
                        VALUES (@baslik, @yazar, @isbn, @yil, @tur, @stok, @stok, @raf)", conn);
                }
                
                cmd.Parameters.AddWithValue("@baslik", txtBaslik.Text.Trim());
                cmd.Parameters.AddWithValue("@yazar", txtYazar.Text.Trim());
                cmd.Parameters.AddWithValue("@isbn", string.IsNullOrEmpty(txtISBN.Text) ? DBNull.Value : txtISBN.Text);
                cmd.Parameters.AddWithValue("@yil", int.TryParse(txtYil.Text, out var yil) ? yil : DBNull.Value);
                cmd.Parameters.AddWithValue("@tur", cmbTur.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@stok", int.TryParse(txtStok.Text, out var stok) ? stok : 1);
                cmd.Parameters.AddWithValue("@raf", string.IsNullOrEmpty(txtRaf.Text) ? DBNull.Value : txtRaf.Text);
                
                cmd.ExecuteNonQuery();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt yapılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Iptal_Click(object sender, RoutedEventArgs e) => Close();
    }
}
