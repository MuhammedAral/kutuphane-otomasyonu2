using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace KutuphaneOtomasyon
{
    public static class DatabaseHelper
    {
        // Connection string sabitleri
        private const string Server = "tcp:127.0.0.1,1433";
        private const string Database = "KutuphaneDB";
        private const string Username = "sa";
        private const string Password = "YourStrong@Password123";
        
        // Connection pooling otomatik olarak ADO.NET tarafından yönetilir
        private static readonly string ConnectionString = 
            $"Server={Server};Database={Database};User Id={Username};Password={Password};TrustServerCertificate=True;Connection Timeout=30;";
        
        private static readonly string MasterConnectionString = 
            $"Server={Server};Database=master;User Id={Username};Password={Password};TrustServerCertificate=True;Connection Timeout=30;";
        
        public static SqlConnection GetConnection() => new(ConnectionString);
        
        public static void Initialize()
        {
            CreateDatabase();
            CreateTables();
            CreateDefaultAdmin();
            CreateDefaultSettings();
        }
        
        /// <summary>
        /// Veritabanı yoksa oluşturur
        /// </summary>
        private static void CreateDatabase()
        {
            using var conn = new SqlConnection(MasterConnectionString);
            conn.Open();
            
            using var cmd = new SqlCommand($@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{Database}')
                CREATE DATABASE {Database}", conn);
            cmd.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Gerekli tabloları oluşturur
        /// </summary>
        private static void CreateTables()
        {
            using var conn = GetConnection();
            conn.Open();
            
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Kullanicilar' AND xtype='U')
                CREATE TABLE Kullanicilar (
                    KullaniciID INT IDENTITY(1,1) PRIMARY KEY,
                    KullaniciAdi NVARCHAR(50) UNIQUE NOT NULL,
                    Sifre NVARCHAR(256) NOT NULL,
                    AdSoyad NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(100),
                    Telefon NVARCHAR(20),
                    Rol NVARCHAR(20) DEFAULT 'Uye',
                    AktifMi BIT DEFAULT 1,
                    OlusturmaTarihi DATETIME DEFAULT GETDATE()
                );
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='KitapTurleri' AND xtype='U')
                CREATE TABLE KitapTurleri (
                    TurID INT IDENTITY(1,1) PRIMARY KEY,
                    TurAdi NVARCHAR(50) NOT NULL
                );
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Kitaplar' AND xtype='U')
                CREATE TABLE Kitaplar (
                    KitapID INT IDENTITY(1,1) PRIMARY KEY,
                    Baslik NVARCHAR(200) NOT NULL,
                    Yazar NVARCHAR(100) NOT NULL,
                    ISBN NVARCHAR(20),
                    Barkod NVARCHAR(50),
                    YayinYili INT,
                    TurID INT FOREIGN KEY REFERENCES KitapTurleri(TurID),
                    StokAdedi INT DEFAULT 1,
                    MevcutAdet INT DEFAULT 1,
                    RafNo NVARCHAR(20),
                    SiraNo NVARCHAR(20),
                    Aciklama NVARCHAR(500),
                    EklenmeTarihi DATETIME DEFAULT GETDATE()
                );
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OduncIslemleri' AND xtype='U')
                CREATE TABLE OduncIslemleri (
                    IslemID INT IDENTITY(1,1) PRIMARY KEY,
                    KitapID INT FOREIGN KEY REFERENCES Kitaplar(KitapID),
                    UyeID INT FOREIGN KEY REFERENCES Kullanicilar(KullaniciID),
                    OduncTarihi DATETIME DEFAULT GETDATE(),
                    BeklenenIadeTarihi DATETIME,
                    IadeTarihi DATETIME,
                    Durum NVARCHAR(20) DEFAULT 'Odunc'
                );
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Ayarlar' AND xtype='U')
                CREATE TABLE Ayarlar (
                    AyarID INT IDENTITY(1,1) PRIMARY KEY,
                    AyarAdi NVARCHAR(50) UNIQUE NOT NULL,
                    AyarDegeri NVARCHAR(100) NOT NULL,
                    Aciklama NVARCHAR(200)
                );
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Degerlendirmeler' AND xtype='U')
                CREATE TABLE Degerlendirmeler (
                    DegerlendirmeID INT IDENTITY(1,1) PRIMARY KEY,
                    UyeID INT FOREIGN KEY REFERENCES Kullanicilar(KullaniciID),
                    KitapID INT FOREIGN KEY REFERENCES Kitaplar(KitapID),
                    Puan TINYINT CHECK (Puan >= 1 AND Puan <= 5),
                    Yorum NVARCHAR(500),
                    Tarih DATETIME DEFAULT GETDATE()
                );
                
                -- Kitaplar tablosuna SiraNo sütunu ekle (eğer yoksa)
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Kitaplar') AND name = 'SiraNo')
                ALTER TABLE Kitaplar ADD SiraNo NVARCHAR(20);";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
            
            FixDatabaseSchema();
        }
        
        /// <summary>
        /// Veritabanı şemasındaki sorunları düzeltir
        /// </summary>
        private static void FixDatabaseSchema()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                
                // ISBN üzerindeki UNIQUE constraint'i kaldır
                const string sql = @"
                    DECLARE @ConstraintName NVARCHAR(200)
                    
                    SELECT TOP 1 @ConstraintName = kc.name
                    FROM sys.key_constraints kc
                    JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE kc.type = 'UQ' AND object_name(kc.parent_object_id) = 'Kitaplar'

                    IF @ConstraintName IS NOT NULL
                    BEGIN
                        DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE Kitaplar DROP CONSTRAINT ' + @ConstraintName
                        EXEC sp_executesql @SQL
                    END";
                
                using (var cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
                
                // Bozuk yıl verilerini temizle
                using (var cmd = new SqlCommand("UPDATE Kitaplar SET YayinYili = NULL WHERE YayinYili < 1000", conn))
                    cmd.ExecuteNonQuery();

                // OduncIslemleri tablosuna CezaMiktari sütunu ekle
                using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('OduncIslemleri') AND name = 'CezaMiktari'", conn))
                {
                    if ((int)checkCmd.ExecuteScalar() == 0)
                    {
                        using var alterCmd = new SqlCommand("ALTER TABLE OduncIslemleri ADD CezaMiktari DECIMAL(10,2) DEFAULT 0", conn);
                        alterCmd.ExecuteNonQuery();
                    }
                }
            }
            catch 
            { 
                // Schema düzeltme hataları kritik değil, uygulama devam edebilir
            }
        }
        
        /// <summary>
        /// Varsayılan admin ve kitap türlerini oluşturur
        /// </summary>
        private static void CreateDefaultAdmin()
        {
            using var conn = GetConnection();
            conn.Open();
            
            // Admin kontrolü ve ekleme
            using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn))
            {
                checkCmd.Parameters.AddWithValue("@user", "admin");
                if ((int)checkCmd.ExecuteScalar() == 0)
                {
                    var hash = HashPassword("admin123");
                    using var insertCmd = new SqlCommand(
                        "INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Rol) VALUES (@user, @sifre, @ad, @rol)", conn);
                    insertCmd.Parameters.AddWithValue("@user", "admin");
                    insertCmd.Parameters.AddWithValue("@sifre", hash);
                    insertCmd.Parameters.AddWithValue("@ad", "Sistem Yöneticisi");
                    insertCmd.Parameters.AddWithValue("@rol", "Yonetici");
                    insertCmd.ExecuteNonQuery();
                }
            }
            
            // Varsayılan kitap türleri - Parameterized query ile
            var turler = new[] { "Roman", "Hikaye", "Şiir", "Tarih", "Bilim", "Felsefe", "Çocuk", "Eğitim" };
            foreach (var tur in turler)
            {
                using var checkTur = new SqlCommand("SELECT COUNT(*) FROM KitapTurleri WHERE TurAdi = @tur", conn);
                checkTur.Parameters.AddWithValue("@tur", tur);
                
                if ((int)checkTur.ExecuteScalar() == 0)
                {
                    using var insertTur = new SqlCommand("INSERT INTO KitapTurleri (TurAdi) VALUES (@tur)", conn);
                    insertTur.Parameters.AddWithValue("@tur", tur);
                    insertTur.ExecuteNonQuery();
                }
            }
        }
        
        /// <summary>
        /// Varsayılan sistem ayarlarını oluşturur
        /// </summary>
        private static void CreateDefaultSettings()
        {
            using var conn = GetConnection();
            conn.Open();
            
            // Varsayılan ayarlar
            var ayarlar = new Dictionary<string, (string Deger, string Aciklama)>
            {
                { "GecikmeUcreti", ("1.00", "Gün başına gecikme ücreti (TL)") },
                { "MaxOduncGun", ("14", "Maksimum ödünç verme süresi (gün)") }
            };
            
            foreach (var ayar in ayarlar)
            {
                using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Ayarlar WHERE AyarAdi = @ad", conn);
                checkCmd.Parameters.AddWithValue("@ad", ayar.Key);
                
                if ((int)checkCmd.ExecuteScalar() == 0)
                {
                    using var insertCmd = new SqlCommand(
                        "INSERT INTO Ayarlar (AyarAdi, AyarDegeri, Aciklama) VALUES (@ad, @deger, @aciklama)", conn);
                    insertCmd.Parameters.AddWithValue("@ad", ayar.Key);
                    insertCmd.Parameters.AddWithValue("@deger", ayar.Value.Deger);
                    insertCmd.Parameters.AddWithValue("@aciklama", ayar.Value.Aciklama);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
        
        /// <summary>
        /// SHA256 ile şifre hash'ler
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
        
        /// <summary>
        /// Kullanıcı girişini doğrular
        /// </summary>
        public static (bool Success, string? Message, int? UserId, string? AdSoyad, string? Rol) VerifyLogin(string username, string password)
        {
            using var conn = GetConnection();
            conn.Open();
            
            var hash = HashPassword(password);
            using var cmd = new SqlCommand(
                "SELECT KullaniciID, AdSoyad, Rol FROM Kullanicilar WHERE KullaniciAdi = @user AND Sifre = @pass AND AktifMi = 1", conn);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", hash);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (true, null, reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
            }
            return (false, "Kullanıcı adı veya şifre hatalı!", null, null, null);
        }
        
        /// <summary>
        /// Kullanıcı adının var olup olmadığını kontrol eder
        /// </summary>
        public static bool IsUsernameExists(string username)
        {
            using var conn = GetConnection();
            conn.Open();
            
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
            cmd.Parameters.AddWithValue("@user", username);
            return (int)cmd.ExecuteScalar() > 0;
        }
        
        /// <summary>
        /// Ayar değerini getirir
        /// </summary>
        public static string GetAyar(string ayarAdi)
        {
            using var conn = GetConnection();
            conn.Open();
            
            using var cmd = new SqlCommand("SELECT AyarDegeri FROM Ayarlar WHERE AyarAdi = @ad", conn);
            cmd.Parameters.AddWithValue("@ad", ayarAdi);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? string.Empty;
        }
        
        /// <summary>
        /// Ayar değerini günceller
        /// </summary>
        public static void SetAyar(string ayarAdi, string ayarDegeri)
        {
            using var conn = GetConnection();
            conn.Open();
            
            using var cmd = new SqlCommand("UPDATE Ayarlar SET AyarDegeri = @deger WHERE AyarAdi = @ad", conn);
            cmd.Parameters.AddWithValue("@ad", ayarAdi);
            cmd.Parameters.AddWithValue("@deger", ayarDegeri);
            cmd.ExecuteNonQuery();
        }
        
        // Varsayılan değerler
        private const decimal DefaultGecikmeUcreti = 1.00m;
        private const int DefaultMaxOduncGun = 14;
        
        /// <summary>
        /// Gecikme ücretini getirir
        /// </summary>
        public static decimal GetGecikmeUcreti()
        {
            var deger = GetAyar("GecikmeUcreti");
            return decimal.TryParse(deger.Replace(",", "."), System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var ucret) ? ucret : DefaultGecikmeUcreti;
        }
        
        /// <summary>
        /// Maksimum ödünç gününü getirir
        /// </summary>
        public static int GetMaxOduncGun()
        {
            var deger = GetAyar("MaxOduncGun");
            return int.TryParse(deger, out var gun) ? gun : DefaultMaxOduncGun;
        }
    }
}
