using Npgsql;
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
            DataContext = this;
            Loaded += async (s, e) => await InitializeAsync();
        }
        
        private async Task InitializeAsync()
        {
            await LoadTurlerAsync();
            await LoadKitaplarAsync();
        }
        
        private async Task LoadTurlerAsync()
        {
            try
            {
                await using var conn = DatabaseHelper.GetConnection();
                await conn.OpenAsync();
                
                var dt = new DataTable();
                dt.Columns.Add("TurID", typeof(int));
                dt.Columns.Add("TurAdi", typeof(string));
                dt.Rows.Add(0, "Tüm Türler");
                
                await using var cmd = new NpgsqlCommand("SELECT TurID, TurAdi FROM KitapTurleri ORDER BY TurAdi", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
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
        
        private async Task LoadKitaplarAsync(string search = "")
        {
            try
            {
                KitaplarListesi.Clear();

                await using var conn = DatabaseHelper.GetConnection();
                await conn.OpenAsync();
                
                var query = @"
                    SELECT k.KitapID, k.Baslik, k.Yazar, COALESCE(k.ISBN, '') as ISBN, 
                           COALESCE(kt.TurAdi, '-') as TurAdi, k.YayinYili, k.StokAdedi, k.MevcutAdet, k.TurID
                    FROM Kitaplar k
                    LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                    WHERE 1=1";
                
                if (!string.IsNullOrEmpty(search))
                    query += " AND (k.Baslik LIKE @search OR k.Yazar LIKE @search OR k.ISBN LIKE @search)";
                
                var selectedTur = cmbTur?.SelectedItem as DataRowView;
                if (selectedTur != null && Convert.ToInt32(selectedTur["TurID"]) > 0)
                    query += " AND k.TurID = @turId";
                
                if (cmbStok?.SelectedIndex == 1)
                    query += " AND k.MevcutAdet > 0";
                else if (cmbStok?.SelectedIndex == 2)
                    query += " AND k.MevcutAdet = 0";
                
                query += " ORDER BY k.KitapID DESC";
                
                await using var cmd = new NpgsqlCommand(query, conn);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                if (selectedTur != null && Convert.ToInt32(selectedTur["TurID"]) > 0)
                    cmd.Parameters.AddWithValue("@turId", selectedTur["TurID"]);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    KitaplarListesi.Add(new KitapItem
                    {
                        KitapID = Convert.ToInt32(reader["KitapID"]),
                        Baslik = reader["Baslik"].ToString(),
                        Yazar = reader["Yazar"].ToString(),
                        ISBN = reader["ISBN"].ToString(),
                        TurAdi = reader["TurAdi"].ToString(),
                        YayinYili = reader["YayinYili"] != DBNull.Value ? Convert.ToInt32(reader["YayinYili"]) : null,
                        StokAdedi = Convert.ToInt32(reader["StokAdedi"]),
                        MevcutAdet = Convert.ToInt32(reader["MevcutAdet"]),
                        TurID = reader["TurID"] != DBNull.Value ? Convert.ToInt32(reader["TurID"]) : 0
                    });
                }
                
                dgKitaplar.ItemsSource = KitaplarListesi;
                txtSonuc.Text = $"{KitaplarListesi.Count} kitap bulundu";
                btnTopluSil.Visibility = Visibility.Collapsed;
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

        private async void TopluSil_Click(object sender, RoutedEventArgs e)
        {
            var seciliKitaplar = KitaplarListesi.Where(k => k.IsSelected).ToList();
            if (seciliKitaplar.Count == 0) return;

            try
            {
                // Onay al
                if (MessageBox.Show($"{seciliKitaplar.Count} kitabı ve ilişkili kayıtları kalıcı olarak silmek istiyor musunuz?", 
                    "Toplu Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;
                
                var kitapIds = seciliKitaplar.Select(k => k.KitapID).ToList();
                int silinen = 0;
                
                // Arka planda sil - TEK SORGU ile toplu silme (çok daha hızlı!)
                await Task.Run(() =>
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    
                    // ID listesini virgülle ayır
                    var idList = string.Join(",", kitapIds);
                    
                    // 1. Değerlendirmeleri toplu sil
                    using (var cmd = new NpgsqlCommand($"DELETE FROM Degerlendirmeler WHERE KitapID IN ({idList})", conn))
                        cmd.ExecuteNonQuery();
                    
                    // 2. Ödünç kayıtlarını toplu sil
                    using (var cmd = new NpgsqlCommand($"DELETE FROM OduncIslemleri WHERE KitapID IN ({idList})", conn))
                        cmd.ExecuteNonQuery();
                    
                    // 3. Kitapları toplu sil
                    using (var cmd = new NpgsqlCommand($"DELETE FROM Kitaplar WHERE KitapID IN ({idList})", conn))
                        silinen = cmd.ExecuteNonQuery();
                });

                await LoadKitaplarAsync();
                MessageBox.Show($"{silinen} kitap ve ilişkili kayıtlar silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) _ = LoadKitaplarAsync(txtSearch?.Text?.Trim() ?? "");
        }
        
        private void YeniKitap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new KitapDialog();
                if (dialog.ShowDialog() == true)
                    _ = LoadKitaplarAsync();
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
                    _ = LoadKitaplarAsync();
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
                
                using var checkActiveCmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM OduncIslemleri WHERE KitapID = @id AND IadeTarihi IS NULL", conn);
                checkActiveCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                int activeLoans = Convert.ToInt32(checkActiveCmd.ExecuteScalar());
                
                if (activeLoans > 0)
                {
                    MessageBox.Show($"Bu kitap şu anda {activeLoans} kişide ödünçte!\n\n" +
                        "Önce ödünç işlemlerinin tamamlanması gerekiyor.", 
                        "Silinemez", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Geçmiş ödünç kayıtları var mı?
                using var checkHistoryCmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM OduncIslemleri WHERE KitapID = @id", conn);
                checkHistoryCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                int historyCount = Convert.ToInt32(checkHistoryCmd.ExecuteScalar());
                
                string message = $"'{kitap.Baslik}' kitabını silmek istiyor musunuz?";
                if (historyCount > 0)
                {
                    message += $"\n\n⚠️ Bu kitaba ait {historyCount} ödünç kaydı da silinecek!";
                }
                
                if (MessageBox.Show(message, "Silme Onayı",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // 1. Önce değerlendirmeleri sil
                    using var deleteRatingsCmd = new NpgsqlCommand(
                        "DELETE FROM Degerlendirmeler WHERE KitapID = @id", conn);
                    deleteRatingsCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    deleteRatingsCmd.ExecuteNonQuery();
                    
                    // 2. Sonra ödünç kayıtlarını sil
                    using var deleteLoansCmd = new NpgsqlCommand(
                        "DELETE FROM OduncIslemleri WHERE KitapID = @id", conn);
                    deleteLoansCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    deleteLoansCmd.ExecuteNonQuery();
                    
                    // 3. Son olarak kitabı sil
                    using var deleteBookCmd = new NpgsqlCommand(
                        "DELETE FROM Kitaplar WHERE KitapID = @id", conn);
                    deleteBookCmd.Parameters.AddWithValue("@id", kitap.KitapID);
                    deleteBookCmd.ExecuteNonQuery();
                    
                    _ = LoadKitaplarAsync();
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


        private async void ExcelImport_Click(object sender, RoutedEventArgs e)
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
                    var fileName = openDialog.FileName;

                    // Arka planda çalıştır - UI donmayacak
                    await Task.Run(async () =>
                    {
                        await using var conn = DatabaseHelper.GetConnection();
                        await conn.OpenAsync();
                        
                        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            var lines = System.IO.File.ReadAllLines(fileName, System.Text.Encoding.UTF8);
                            int rowNum = 1;
                            foreach (var line in lines.Skip(1))
                            {
                                rowNum++;
                                var parts = line.Split(',');
                                if (parts.Length < 4)
                                {
                                    hatali++;
                                    continue;
                                }
                                
                                var baslik = parts[0].Trim();
                                var yazar = parts[1].Trim();
                                var barkod = parts[3].Trim(); 
                                
                                if (string.IsNullOrEmpty(barkod) && parts.Length > 2)
                                    barkod = parts[2].Trim();

                                if (string.IsNullOrEmpty(baslik)) continue;

                                if (string.IsNullOrEmpty(barkod) || barkod.Length != 13 || !long.TryParse(barkod, out _))
                                {
                                    hatali++;
                                    hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Geçersiz barkod ({barkod})");
                                    continue;
                                }

                                if (await IsBarcodeExistsAsync(conn, barkod))
                                {
                                    hatali++;
                                    hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Mükerrer barkod ({barkod})");
                                    continue;
                                }

                                var yilStr = parts.Length > 4 ? parts[4].Trim() : "";
                                var stokStr = parts.Length > 6 ? parts[6].Trim() : "1";
                                var rafNo = parts.Length > 7 ? parts[7].Trim() : "";
                                var siraNo = parts.Length > 8 ? parts[8].Trim() : "";

                                int.TryParse(yilStr, out var yil);
                                int.TryParse(stokStr, out var stok);
                                if (stok <= 0) stok = 1;
                                
                                await InsertKitapAsync(conn, baslik, yazar, yil, stok, barkod, rafNo, siraNo);
                                eklenen++;
                            }
                        }
                        else
                        {
                            using var workbook = new XLWorkbook(fileName);
                            var worksheet = workbook.Worksheet(1);
                            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);
                            
                            int rowNum = 1;
                            foreach (var row in rows)
                            {
                                rowNum++;
                                var baslik = row.Cell(1).GetString().Trim();
                                var yazar = row.Cell(2).GetString().Trim();
                                var barkod = row.Cell(4).GetString().Trim();
                                
                                if (string.IsNullOrEmpty(barkod))
                                    barkod = row.Cell(3).GetString().Trim();

                                if (string.IsNullOrEmpty(baslik)) continue;
                                
                                if (string.IsNullOrEmpty(barkod) || barkod.Length != 13 || !long.TryParse(barkod, out _))
                                {
                                    hatali++;
                                    hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Geçersiz barkod ({barkod})");
                                    continue;
                                }

                                if (await IsBarcodeExistsAsync(conn, barkod))
                                {
                                    hatali++;
                                    hataListesi.AppendLine($"Satır {rowNum}: '{baslik}' - Mükerrer barkod ({barkod})");
                                    continue;
                                }
                                
                                var yilStr = row.Cell(5).GetString().Trim(); 
                                var stokStr = row.Cell(7).GetString().Trim(); 
                                var rafNo = row.Cell(8).GetString().Trim();
                                var siraNo = row.Cell(9).GetString().Trim();

                                int.TryParse(yilStr, out var yil);
                                int.TryParse(stokStr, out var stok);
                                if (stok <= 0) stok = 1;
                                
                                await InsertKitapAsync(conn, baslik, yazar, yil, stok, barkod, rafNo, siraNo);
                                eklenen++;
                            }
                        }
                    });
                    
                    await LoadKitaplarAsync();

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

        private async Task<bool> IsBarcodeExistsAsync(NpgsqlConnection conn, string barcode)
        {
            await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Kitaplar WHERE ISBN = @barcode", conn);
            cmd.Parameters.AddWithValue("@barcode", barcode);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
        
        private async Task InsertKitapAsync(NpgsqlConnection conn, string baslik, string yazar, int yil, int stok, string barkod, string rafNo, string siraNo)
        {
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO Kitaplar (Baslik, Yazar, YayinYili, StokAdedi, MevcutAdet, ISBN, TurID, RafNo, SiraNo)
                VALUES (@baslik, @yazar, @yil, @stok, @stok, @isbn, 1, @raf, @sira)", conn); 
            
            cmd.Parameters.AddWithValue("@baslik", baslik);
            cmd.Parameters.AddWithValue("@yazar", yazar);
            cmd.Parameters.AddWithValue("@yil", yil > 0 ? yil : DBNull.Value);
            cmd.Parameters.AddWithValue("@stok", stok);
            cmd.Parameters.AddWithValue("@isbn", barkod);
            cmd.Parameters.AddWithValue("@raf", string.IsNullOrEmpty(rafNo) ? DBNull.Value : rafNo);
            cmd.Parameters.AddWithValue("@sira", string.IsNullOrEmpty(siraNo) ? DBNull.Value : siraNo);
            
            await cmd.ExecuteNonQueryAsync();
        }
        
        private async void ExcelExport_Click(object sender, RoutedEventArgs e)
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
                    var fileName = saveDialog.FileName;
                    int rowCount = 0;
                    
                    await Task.Run(async () =>
                    {
                        await using var conn = DatabaseHelper.GetConnection();
                        await conn.OpenAsync();
                        
                        var query = @"
                            SELECT k.KitapID as ID, k.Baslik as KitapAdi, k.Yazar, 
                                   COALESCE(k.ISBN, '') as ISBN, k.YayinYili as YayinYili,
                                   COALESCE(kt.TurAdi, '') as Tur, k.StokAdedi as Stok, 
                                   k.MevcutAdet as Mevcut, COALESCE(k.RafNo, '') as RafNo,
                                   COALESCE(k.SiraNo, '') as SiraNo
                            FROM Kitaplar k
                            LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                            ORDER BY k.KitapID";
                        
                        await using var cmd = new NpgsqlCommand(query, conn);
                        await using var reader = await cmd.ExecuteReaderAsync();
                        var dt = new DataTable();
                        dt.Load(reader);
                        rowCount = dt.Rows.Count;
                        
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("Kitaplar");
                        worksheet.Cell(1, 1).InsertTable(dt);
                        
                        worksheet.Columns().AdjustToContents();
                        
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
                        headerRow.Style.Font.FontColor = XLColor.White;
                        
                        workbook.SaveAs(fileName);
                    });
                    
                    MessageBox.Show($"{rowCount} kitap Excel'e aktarıldı!\n\n{fileName}", 
                        "Dışa Aktarma Tamamlandı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => _ = LoadKitaplarAsync(txtSearch.Text.Trim());
        
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) _ = LoadKitaplarAsync(txtSearch?.Text?.Trim() ?? "");
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




