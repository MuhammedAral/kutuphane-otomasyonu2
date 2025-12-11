using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace KutuphaneOtomasyon.Pages
{
    public partial class KitaplarPage : Page
    {
        public ObservableCollection<KitapItem> KitaplarListesi { get; set; } = new ObservableCollection<KitapItem>();

        public KitaplarPage()
        {
            InitializeComponent();
            DataContext = this; // DataContext'i bu sayfaya bağla
            LoadTurler();
            LoadKitaplar();
        }
        
        private void LoadTurler()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var dt = new DataTable();
                dt.Columns.Add("TurID", typeof(int));
                dt.Columns.Add("TurAdi", typeof(string));
                dt.Rows.Add(0, "Tüm Türler");
                
                using var cmd = new SqlCommand("SELECT TurID, TurAdi FROM KitapTurleri ORDER BY TurAdi", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    dt.Rows.Add(reader["TurID"], reader["TurAdi"]);
                }
                
                cmbTur.ItemsSource = dt.DefaultView;
                cmbTur.SelectedIndex = 0;
            }
            catch (Exception)
            {
                // Tür listesi yüklenemezse uygulama çalışmaya devam eder
            }
        }
        
        private void LoadKitaplar(string search = "")
        {
            try
            {
                KitaplarListesi.Clear();

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                var query = @"
                    SELECT k.KitapID, k.Baslik, k.Yazar, ISNULL(k.ISBN, '') as ISBN, 
                           ISNULL(kt.TurAdi, '-') as TurAdi, k.YayinYili, k.StokAdedi, k.MevcutAdet, k.TurID
                    FROM Kitaplar k
                    LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                    WHERE 1=1";
                
                // Arama filtresi
                if (!string.IsNullOrEmpty(search))
                    query += " AND (k.Baslik LIKE @search OR k.Yazar LIKE @search OR k.ISBN LIKE @search)";
                
                // Tür filtresi
                var selectedTur = cmbTur?.SelectedItem as DataRowView;
                if (selectedTur != null && Convert.ToInt32(selectedTur["TurID"]) > 0)
                    query += " AND k.TurID = @turId";
                
                // Stok filtresi
                if (cmbStok?.SelectedIndex == 1) // Stokta var
                    query += " AND k.MevcutAdet > 0";
                else if (cmbStok?.SelectedIndex == 2) // Stok yok
                    query += " AND k.MevcutAdet = 0";
                
                query += " ORDER BY k.KitapID DESC";
                
                using var cmd = new SqlCommand(query, conn);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                if (selectedTur != null && Convert.ToInt32(selectedTur["TurID"]) > 0)
                    cmd.Parameters.AddWithValue("@turId", selectedTur["TurID"]);
                
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    KitaplarListesi.Add(new KitapItem
                    {
                        KitapID = (int)reader["KitapID"],
                        Baslik = reader["Baslik"].ToString(),
                        Yazar = reader["Yazar"].ToString(),
                        ISBN = reader["ISBN"].ToString(),
                        TurAdi = reader["TurAdi"].ToString(),
                        YayinYili = reader["YayinYili"] != DBNull.Value ? (int)reader["YayinYili"] : null,
                        StokAdedi = (int)reader["StokAdedi"],
                        MevcutAdet = (int)reader["MevcutAdet"],
                        TurID = reader["TurID"] != DBNull.Value ? (int)reader["TurID"] : 0
                    });
                }
                
                // DataGrid ItemsSource'u XAML'den veya buradan bağla. 
                // Biz XAML'de ItemsSource="{Binding KitaplarListesi}" kullanacağız ama code-behind'dan set etmek de garanti olur.
                dgKitaplar.ItemsSource = KitaplarListesi;
                
                txtSonuc.Text = $"{KitaplarListesi.Count} kitap bulundu";
                btnTopluSil.Visibility = Visibility.Collapsed; // Başlangıçta gizle
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kitaplar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sürükleme işlemi sırasında hedef durum (Seçilecek mi, Silinecek mi?)
        // null: Sürükleme yok
        // true: Seçme modu
        // false: Silme modu
        private bool? _targetSelectionState = null;

        private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is KitapItem item)
            {
                // Checkbox kontrolü (Native davranışa karışma)
                if (e.OriginalSource is System.Windows.Controls.Primitives.ToggleButton || 
                    (e.OriginalSource is FrameworkElement fe && fe.Parent is CheckBox))
                {
                    return; 
                }
                
                // Button kontrolü - İşlem butonlarına tıklandığında satır seçimini engelleme
                var source = e.OriginalSource as DependencyObject;
                while (source != null && source != row)
                {
                    if (source is Button)
                    {
                        return; // Buton tıklandığında müdahale etme
                    }
                    source = VisualTreeHelper.GetParent(source);
                }

                // Tıklanılan satırın ŞU ANKİ durumunu al
                // Eğer zaten seçiliyse -> Hedef: SİLMEK (false)
                // Eğer seçili değilse -> Hedef: SEÇMEK (true)
                _targetSelectionState = !item.IsSelected;

                // İlk satırı hemen güncelle (Tıklama hissi için)
                item.IsSelected = _targetSelectionState.Value;
                
                UpdateTopluSilButton();
                
                // DataGrid'in native seçimini engelle ve fareyi yakala (Sürükleme takibi için)
                e.Handled = true; 
            }
        }

        private void DataGridRow_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Sürükleme devam ediyor mu?
            if (e.LeftButton == MouseButtonState.Pressed && _targetSelectionState.HasValue)
            {
                if (sender is DataGridRow row && row.DataContext is KitapItem item)
                {
                    // Satırı hedef duruma getir (Sadece farklıysa güncelle)
                    if (item.IsSelected != _targetSelectionState.Value)
                    {
                        item.IsSelected = _targetSelectionState.Value;
                        UpdateTopluSilButton();
                    }
                }
            }
        }

        // Fare bırakıldığında modu sıfırla
        private void DataGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _targetSelectionState = null;
        }
        
        private void UpdateTopluSilButton()
        {
            int seciliSayisi = KitaplarListesi.Count(k => k.IsSelected);
            if (seciliSayisi > 0)
            {
                btnTopluSil.Visibility = Visibility.Visible;
                txtTopluSil.Text = $"{seciliSayisi} Kitabı Sil";
                txtTopluSil.Visibility = Visibility.Visible;
            }
            else
            {
                btnTopluSil.Visibility = Visibility.Collapsed;
                txtTopluSil.Visibility = Visibility.Collapsed;
            }
        }

        // Header'daki checkbox değişince
        private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk)
            {
                bool val = chk.IsChecked ?? false;
                
                // Tüm veri listesini güncelle
                foreach (var kitap in KitaplarListesi)
                {
                    kitap.IsSelected = val;
                }
                
                // Görsel seçimi de senkronize et (İsteğe bağlı ama şık durur)
                if (!val) dgKitaplar.SelectedItems.Clear();

                UpdateTopluSilButton();
            }
        }

        private void TopluSil_Click(object sender, RoutedEventArgs e)
        {
            var seciliKitaplar = KitaplarListesi.Where(k => k.IsSelected).ToList();
            if (seciliKitaplar.Count == 0) return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                // Aktif ödünçteki kitapları kontrol et
                var activeBookIds = new List<int>();
                foreach (var kitap in seciliKitaplar)
                {
                    using var checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM OduncIslemleri WHERE KitapID = @id AND IadeTarihi IS NULL", conn);
                    checkCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        activeBookIds.Add(kitap.KitapID);
                    }
                }
                
                if (activeBookIds.Count > 0)
                {
                    var silinebilir = seciliKitaplar.Count - activeBookIds.Count;
                    var result = MessageBox.Show(
                        $"{activeBookIds.Count} kitap şu anda ödünçte ve silinemez!\n\n" +
                        $"Geri kalan {silinebilir} kitabı silmek ister misiniz?",
                        "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes) return;
                    
                    // Aktif olanları listeden çıkar
                    seciliKitaplar = seciliKitaplar.Where(k => !activeBookIds.Contains(k.KitapID)).ToList();
                }
                else
                {
                    if (MessageBox.Show($"{seciliKitaplar.Count} kitabı ve ilişkili kayıtları kalıcı olarak silmek istiyor musunuz?", 
                        "Toplu Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;
                }
                
                int silinen = 0;
                foreach (var kitap in seciliKitaplar)
                {
                    // 1. Değerlendirmeleri sil
                    using (var cmd = new SqlCommand("DELETE FROM Degerlendirmeler WHERE KitapID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", kitap.KitapID);
                        cmd.ExecuteNonQuery();
                    }
                    
                    // 2. Ödünç kayıtlarını sil
                    using (var cmd = new SqlCommand("DELETE FROM OduncIslemleri WHERE KitapID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", kitap.KitapID);
                        cmd.ExecuteNonQuery();
                    }
                    
                    // 3. Kitabı sil
                    using (var cmd = new SqlCommand("DELETE FROM Kitaplar WHERE KitapID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", kitap.KitapID);
                        cmd.ExecuteNonQuery();
                        silinen++;
                    }
                }

                LoadKitaplar();
                MessageBox.Show($"{silinen} kitap ve ilişkili kayıtlar silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadKitaplar(txtSearch?.Text?.Trim() ?? "");
        }
        
        private void YeniKitap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new KitapDialog();
                if (dialog.ShowDialog() == true)
                    LoadKitaplar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private KitapItem? GetKitapFromButton(object sender)
        {
            if (sender is Button btn && btn.DataContext is KitapItem kitap)
                return kitap;
            return null;
        }
        
        // İşlem butonlarına tıklandığında satır seçimini engellemek için
        private void ActionButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false; // Butonun kendi Click olayının çalışmasına izin ver
        }
        
        private void Detay_Click(object sender, RoutedEventArgs e)
        {
            var kitap = GetKitapFromButton(sender);
            if (kitap == null) return;
            
            var dialog = new KitapDetayDialog(kitap.KitapID);
            dialog.ShowDialog();
        }
        
        private void DataGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgKitaplar.SelectedItem is KitapItem kitap)
            {
                var dialog = new KitapDetayDialog(kitap.KitapID);
                dialog.ShowDialog();
            }
        }
        
        private void Duzenle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var kitap = GetKitapFromButton(sender);
                if (kitap == null) return;
                
                var dialog = new KitapDialog(kitap.KitapID);
                if (dialog.ShowDialog() == true)
                    LoadKitaplar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Sil_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var kitap = GetKitapFromButton(sender);
                if (kitap == null) return;
                
                // Önce bu kitaba ait aktif ödünç var mı kontrol et
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                using var checkActiveCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM OduncIslemleri WHERE KitapID = @id AND IadeTarihi IS NULL", conn);
                checkActiveCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                int activeLoans = (int)checkActiveCmd.ExecuteScalar();
                
                if (activeLoans > 0)
                {
                    MessageBox.Show($"Bu kitap şu anda {activeLoans} kişide ödünçte!\n\n" +
                        "Önce ödünç işlemlerinin tamamlanması gerekiyor.", 
                        "Silinemez", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Geçmiş ödünç kayıtları var mı?
                using var checkHistoryCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM OduncIslemleri WHERE KitapID = @id", conn);
                checkHistoryCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                int historyCount = (int)checkHistoryCmd.ExecuteScalar();
                
                string message = $"'{kitap.Baslik}' kitabını silmek istiyor musunuz?";
                if (historyCount > 0)
                {
                    message += $"\n\n⚠️ Bu kitaba ait {historyCount} ödünç kaydı da silinecek!";
                }
                
                if (MessageBox.Show(message, "Silme Onayı",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // 1. Önce değerlendirmeleri sil
                    using var deleteRatingsCmd = new SqlCommand(
                        "DELETE FROM Degerlendirmeler WHERE KitapID = @id", conn);
                    deleteRatingsCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    deleteRatingsCmd.ExecuteNonQuery();
                    
                    // 2. Sonra ödünç kayıtlarını sil
                    using var deleteLoansCmd = new SqlCommand(
                        "DELETE FROM OduncIslemleri WHERE KitapID = @id", conn);
                    deleteLoansCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    deleteLoansCmd.ExecuteNonQuery();
                    
                    // 3. Son olarak kitabı sil
                    using var deleteBookCmd = new SqlCommand(
                        "DELETE FROM Kitaplar WHERE KitapID = @id", conn);
                    deleteBookCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    deleteBookCmd.ExecuteNonQuery();
                    
                    LoadKitaplar();
                    MessageBox.Show("Kitap ve ilişkili kayıtlar silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Özel sıralama mantığı
            if (e.Column.Header.ToString() == "Yazar")
            {
                e.Handled = true; // Varsayılan sıralamayı iptal et
                
                var view = CollectionViewSource.GetDefaultView(dgKitaplar.ItemsSource);
                if (view == null) return;

                var direction = ListSortDirection.Ascending;
                
                // Mevcut sıralama yönünü kontrol et ve tersine çevir
                if (e.Column.SortDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                
                // Sıralama yönünü güncelle (UI için)
                e.Column.SortDirection = direction;
                
                // Sıralamayı uygula: Önce Yazar, Sonra Başlık
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Yazar", direction));
                view.SortDescriptions.Add(new SortDescription("Baslik", ListSortDirection.Ascending)); // İkincil sıralama hep A-Z olsun
            }
        }
// Import ve Export kodları da uyarlanmış haliyle aşağıda


        private void ExcelImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Title = "Excel veya CSV Dosyası Seç",
                    Filter = "Tüm Desteklenenler|*.xlsx;*.xls;*.csv|Excel Dosyaları|*.xlsx;*.xls|CSV Dosyaları|*.csv",
                    FilterIndex = 1
                };
                
                if (openDialog.ShowDialog() == true)
                {
                    int eklenen = 0;
                    int hatali = 0;
                    var hataListesi = new System.Text.StringBuilder();

                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    
                    if (openDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var lines = System.IO.File.ReadAllLines(openDialog.FileName, System.Text.Encoding.UTF8);
                        int rowNum = 1;
                        foreach (var line in lines.Skip(1))
                        {
                            rowNum++;
                            var parts = line.Split(',');
                            if (parts.Length < 4) // En az Barkod'a kadar kolon olmalı
                            {
                                hatali++;
                                continue;
                            }
                            
                            var baslik = parts[0].Trim();
                            var yazar = parts[1].Trim();
                            // parts[2] ISBN, parts[3] Barkod olarak varsayıyoruz (CSV formatına göre)
                            var barkod = parts[3].Trim(); 
                            
                            // Eğer Barkod boşsa ISBN'i dene
                            if (string.IsNullOrEmpty(barkod) && parts.Length > 2)
                                barkod = parts[2].Trim();

                            if (string.IsNullOrEmpty(baslik)) continue;

                            // BARKOD DOĞRULAMA (13 Haneli ve Dolu Olmalı)
                            if (string.IsNullOrEmpty(barkod) || barkod.Length != 13 || !long.TryParse(barkod, out _))
                            {
                                hatali++;
                                hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Geçersiz veya eksik barkod ({barkod})");
                                continue;
                            }

                            // Veritabanında aynı barkod var mı kontrolü
                            if (IsBarcodeExists(conn, barkod))
                            {
                                hatali++;
                                hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Bu barkod zaten sistemde kayıtlı ({barkod})");
                                continue;
                            }

                            var yilStr = parts.Length > 4 ? parts[4].Trim() : "";
                            var stokStr = parts.Length > 6 ? parts[6].Trim() : "1";
                            var rafNo = parts.Length > 7 ? parts[7].Trim() : "";
                            var siraNo = parts.Length > 8 ? parts[8].Trim() : "";

                            int.TryParse(yilStr, out var yil);
                            int.TryParse(stokStr, out var stok);
                            if (stok <= 0) stok = 1;
                            
                            InsertKitap(conn, baslik, yazar, yil, stok, barkod, rafNo, siraNo);
                            eklenen++;
                        }
                    }
                    else
                    {
                        using var workbook = new XLWorkbook(openDialog.FileName);
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);
                        
                        int rowNum = 1;
                        foreach (var row in rows)
                        {
                            rowNum++;
                            var baslik = row.Cell(1).GetString().Trim();
                            var yazar = row.Cell(2).GetString().Trim();
                            // Col 3: ISBN, Col 4: Barkod
                            var barkod = row.Cell(4).GetString().Trim();
                            
                            if (string.IsNullOrEmpty(barkod))
                                barkod = row.Cell(3).GetString().Trim();

                            if (string.IsNullOrEmpty(baslik)) continue;
                            
                            // BARKOD DOĞRULAMA
                            if (string.IsNullOrEmpty(barkod) || barkod.Length != 13 || !long.TryParse(barkod, out _))
                            {
                                hatali++;
                                hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Geçersiz veya eksik barkod ({barkod})");
                                continue;
                            }

                            // Veritabanında aynı barkod var mı kontrolü
                            if (IsBarcodeExists(conn, barkod))
                            {
                                hatali++;
                                hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Bu barkod zaten sistemde kayıtlı ({barkod})");
                                continue;
                            }
                            
                            var yilStr = row.Cell(5).GetString().Trim(); 
                            // Col 6 is TurID
                            var stokStr = row.Cell(7).GetString().Trim(); 
                            var rafNo = row.Cell(8).GetString().Trim();
                            var siraNo = row.Cell(9).GetString().Trim();

                            int.TryParse(yilStr, out var yil);
                            int.TryParse(stokStr, out var stok);
                            if (stok <= 0) stok = 1;
                            
                            InsertKitap(conn, baslik, yazar, yil, stok, barkod, rafNo, siraNo);
                            eklenen++;
                        }
                    }
                    
                    LoadKitaplar();

                    string sonucMesaji = $"{eklenen} kitap başarıyla eklendi.";
                    if (hatali > 0)
                    {
                        sonucMesaji += $"\n\n⚠️ {hatali} kitap eklenemedi (Barkod hatası veya mükerrer).";
                    }
                    
                    MessageBox.Show(sonucMesaji, "İçe Aktarma Sonucu", MessageBoxButton.OK, hatali > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İçe aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsBarcodeExists(SqlConnection conn, string barcode)
        {
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Kitaplar WHERE ISBN = @barcode", conn);
            cmd.Parameters.AddWithValue("@barcode", barcode);
            return (int)cmd.ExecuteScalar() > 0;
        }
        
        private void InsertKitap(SqlConnection conn, string baslik, string yazar, int yil, int stok, string barkod, string rafNo, string siraNo)
        {
            var cmd = new SqlCommand(@"
                INSERT INTO Kitaplar (Baslik, Yazar, YayinYili, StokAdedi, MevcutAdet, ISBN, TurID, RafNo, SiraNo)
                VALUES (@baslik, @yazar, @yil, @stok, @stok, @isbn, 1, @raf, @sira)", conn); 
            
            cmd.Parameters.AddWithValue("@baslik", baslik);
            cmd.Parameters.AddWithValue("@yazar", yazar);
            cmd.Parameters.AddWithValue("@yil", yil > 0 ? yil : DBNull.Value);
            cmd.Parameters.AddWithValue("@stok", stok);
            cmd.Parameters.AddWithValue("@isbn", barkod);
            cmd.Parameters.AddWithValue("@raf", string.IsNullOrEmpty(rafNo) ? DBNull.Value : rafNo);
            cmd.Parameters.AddWithValue("@sira", string.IsNullOrEmpty(siraNo) ? DBNull.Value : siraNo);
            
            cmd.ExecuteNonQuery();
        }
        
        private void ExcelExport_Click(object sender, RoutedEventArgs e)
        {
             try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Excel Olarak Kaydet",
                    Filter = "Excel Dosyası|*.xlsx",
                    FileName = $"Kitaplar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    
                    var query = @"
                        SELECT k.KitapID as 'ID', k.Baslik as 'Kitap Adı', k.Yazar, 
                               ISNULL(k.ISBN, '') as 'ISBN', k.YayinYili as 'Yayın Yılı',
                               ISNULL(kt.TurAdi, '') as 'Tür', k.StokAdedi as 'Stok', 
                               k.MevcutAdet as 'Mevcut', ISNULL(k.RafNo, '') as 'Raf No',
                               ISNULL(k.SiraNo, '') as 'Sıra No'
                        FROM Kitaplar k
                        LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                        ORDER BY k.KitapID";
                    
                    var adapter = new SqlDataAdapter(query, conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Kitaplar");
                    worksheet.Cell(1, 1).InsertTable(dt);
                    
                    worksheet.Columns().AdjustToContents();
                    
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
                    headerRow.Style.Font.FontColor = XLColor.White;
                    
                    workbook.SaveAs(saveDialog.FileName);
                    
                    MessageBox.Show($"{dt.Rows.Count} kitap Excel'e aktarıldı!\n\n{saveDialog.FileName}", 
                        "Dışa Aktarma Tamamlandı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => LoadKitaplar(txtSearch.Text.Trim());
        
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadKitaplar(txtSearch?.Text?.Trim() ?? "");
        }
    }

    // Modern ViewModel Sınıfı
    public class KitapItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public int KitapID { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Yazar { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string TurAdi { get; set; } = string.Empty;
        public int? YayinYili { get; set; }
        public int StokAdedi { get; set; }
        public int MevcutAdet { get; set; }
        public int TurID { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
