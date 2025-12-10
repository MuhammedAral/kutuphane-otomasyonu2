using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.MemberPages
{
    public partial class ProfilPage : Page
    {
        public ProfilPage(int userId)
        {
            InitializeComponent();
            LoadProfil(userId);
        }
        
        private void LoadProfil(int userId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            var cmd = new SqlCommand("SELECT AdSoyad, KullaniciAdi, Email, Telefon FROM Kullanicilar WHERE KullaniciID = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                AddInfoRow("👤 Ad Soyad", reader["AdSoyad"]?.ToString() ?? "-");
                AddInfoRow("🆔 Kullanıcı Adı", reader["KullaniciAdi"]?.ToString() ?? "-");
                AddInfoRow("📧 Email", reader["Email"]?.ToString() ?? "-");
                AddInfoRow("📱 Telefon", reader["Telefon"]?.ToString() ?? "-");
                AddInfoRow("🔑 Rol", "Üye");
            }
        }
        
        private void AddInfoRow(string label, string value)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 10) };
            sp.Children.Add(new TextBlock { Text = label, Width = 150, Foreground = System.Windows.Media.Brushes.Gray, FontSize = 14 });
            sp.Children.Add(new TextBlock { Text = value, Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 14 });
            infoPanel.Children.Add(sp);
        }
    }
}
