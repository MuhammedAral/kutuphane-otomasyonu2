using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace KutuphaneOtomasyon.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadStats();
            LoadOverdueBooks();
        }

        private void LoadStats()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // 1. Toplam Kitap
                using var cmdBook = new SqlCommand("SELECT COUNT(*) FROM Kitaplar", conn);
                txtTotalBooks.Text = cmdBook.ExecuteScalar()?.ToString() ?? "0";

                // 2. Toplam Üye (Rolü 'Uye' olanlar)
                using var cmdMember = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE Rol = 'Uye'", conn);
                txtTotalMembers.Text = cmdMember.ExecuteScalar()?.ToString() ?? "0";

                // 3. Aktif Ödünç (Henüz iade edilmemiş)
                using var cmdLoan = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL", conn);
                txtActiveLoans.Text = cmdLoan.ExecuteScalar()?.ToString() ?? "0";

                // 4. Gecikenler (Iade edilmemiş VE BeklenenIadeTarihi geçmiş)
                using var cmdOverdue = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL AND BeklenenIadeTarihi < GETDATE()", conn);
                txtOverdueBooks.Text = cmdOverdue.ExecuteScalar()?.ToString() ?? "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstatistikler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOverdueBooks()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // Geciken kitapları detaylı listele
                var query = @"
                    SELECT 
                        k.AdSoyad, 
                        kt.Baslik AS KitapBaslik, 
                        o.BeklenenIadeTarihi,
                        DATEDIFF(day, o.BeklenenIadeTarihi, GETDATE()) AS GecikmeGun
                    FROM OduncIslemleri o
                    JOIN Kullanicilar k ON o.UyeID = k.KullaniciID
                    JOIN Kitaplar kt ON o.KitapID = kt.KitapID
                    WHERE o.IadeTarihi IS NULL AND o.BeklenenIadeTarihi < GETDATE()
                    ORDER BY o.BeklenenIadeTarihi ASC"; // En çok geciken en üstte

                var adapter = new SqlDataAdapter(query, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                dgOverdue.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count == 0)
                {
                    dgOverdue.Visibility = Visibility.Collapsed;
                    txtNoOverdue.Visibility = Visibility.Visible;
                }
                else
                {
                    dgOverdue.Visibility = Visibility.Visible;
                    txtNoOverdue.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gecikme listesi yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hızlı İşlem Butonları (Parent window üzerinden yönlendirme yapılabilir veya direkt dialog açılabilir)
        // Burada basitçe ilgili Dialogları açacağız. Sayfa yönlendirmesi için NavigationService kullanabiliriz ama butonların amacı hızlı işlem.
        
        private void QuickAddBook_Click(object sender, RoutedEventArgs e)
        {
            OpenAddBookDialog();
        }
        
        private void QuickAddBook_Border_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenAddBookDialog();
        }
        
        private void OpenAddBookDialog()
        {
            var dialog = new KitapDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadStats(); // İstatistikleri güncelle
            }
        }

        private void QuickAddMember_Click(object sender, RoutedEventArgs e)
        {
            OpenAddMemberDialog();
        }
        
        private void QuickAddMember_Border_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenAddMemberDialog();
        }
        
        private void OpenAddMemberDialog()
        {
            var dialog = new UyeDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadStats();
            }
        }

        private void QuickLoan_Click(object sender, RoutedEventArgs e)
        {
            OpenLoanDialog();
        }
        
        private void QuickLoan_Border_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenLoanDialog();
        }
        
        private void OpenLoanDialog()
        {
             var dialog = new OduncDialog();
             if (dialog.ShowDialog() == true)
             {
                 LoadStats();
                 LoadOverdueBooks();
             }
        }
    }
}
