using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace KutuphaneOtomasyon
{
    public static class DatabaseHelper
    {
        private static string Server = "localhost";
        private static string Database = "KutuphaneDB";
        private static string Username = "sa";
        private static string Password = "YourStrong@Password123";
        
        private static string ConnectionString => 
            $"Server={Server};Database={Database};User Id={Username};Password={Password};TrustServerCertificate=True;";
        
        private static string MasterConnectionString => 
            $"Server={Server};Database=master;User Id={Username};Password={Password};TrustServerCertificate=True;";
        
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
        
        public static void Initialize()
        {
            CreateDatabase();
            CreateTables();
            CreateDefaultAdmin();
            CreateDefaultSettings();
        }
        
        private static void CreateDatabase()
        {
            using var conn = new SqlConnection(MasterConnectionString);
            conn.Open();
            
            var cmd = new SqlCommand($@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{Database}')
                CREATE DATABASE {Database}", conn);
            cmd.ExecuteNonQuery();
        }
        
        private static void CreateTables()
        {
            using var conn = GetConnection();
            conn.Open();
            
            var sql = @"
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
                
                -- Kitaplar tablosuna SiraNo sütunu ekle (eğer yoksa)
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Kitaplar') AND name = 'SiraNo')
                ALTER TABLE Kitaplar ADD SiraNo NVARCHAR(20);";
            
            new SqlCommand(sql, conn).ExecuteNonQuery();
            
            FixDatabaseSchema();
        }
        
        private static void FixDatabaseSchema()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                
                // ISBN üzerindeki UNIQUE constraint'i bul ve kesin olarak kaldır
                // Bu script, constraint isminden bağımsız olarak Kitaplar tablosundaki tüm UNIQUE constraintleri bulup siler (Sadece ISBN için değil, eğer sorunun kaynağı buysa)
                // Ancak dikkatli olup sadece ISBN ile ilgili olanı hedeflemek daha iyi.
                
                var sql = @"
                    DECLARE @ConstraintName NVARCHAR(200)
                    
                    -- Önce ISBN üzerindeki Unique Constraint'i bul
                    SELECT TOP 1 @ConstraintName = kc.name
                    FROM sys.key_constraints kc
                    JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE kc.type = 'UQ' AND object_name(kc.parent_object_id) = 'Kitaplar'

                    -- Eğer varsa sil
                    IF @ConstraintName IS NOT NULL
                    BEGIN
                        DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE Kitaplar DROP CONSTRAINT ' + @ConstraintName
                        EXEC sp_executesql @SQL
                    END
                ";
                
                new SqlCommand(sql, conn).ExecuteNonQuery();
                
                // Bozuk yıl verilerini (örn: 2, 4, 5) temizle
                new SqlCommand("UPDATE Kitaplar SET YayinYili = NULL WHERE YayinYili < 1000", conn).ExecuteNonQuery();
            }
            catch { /* Hata yutulur */ }
        }
        
        private static void CreateDefaultAdmin()
        {
            using var conn = GetConnection();
            conn.Open();
            
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = 'admin'", conn);
            if ((int)checkCmd.ExecuteScalar() == 0)
            {
                var hash = HashPassword("admin123");
                var insertCmd = new SqlCommand(
                    "INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Rol) VALUES ('admin', @sifre, 'Sistem Yöneticisi', 'Yonetici')", conn);
                insertCmd.Parameters.AddWithValue("@sifre", hash);
                insertCmd.ExecuteNonQuery();
            }
            
            // Varsayılan kitap türleri
            var turler = new[] { "Roman", "Hikaye", "Şiir", "Tarih", "Bilim", "Felsefe", "Çocuk", "Eğitim" };
            foreach (var tur in turler)
            {
                var checkTur = new SqlCommand($"SELECT COUNT(*) FROM KitapTurleri WHERE TurAdi = '{tur}'", conn);
                if ((int)checkTur.ExecuteScalar() == 0)
                {
                    new SqlCommand($"INSERT INTO KitapTurleri (TurAdi) VALUES ('{tur}')", conn).ExecuteNonQuery();
                }
            }
        }
        
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
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Ayarlar WHERE AyarAdi = @ad", conn);
                checkCmd.Parameters.AddWithValue("@ad", ayar.Key);
                if ((int)checkCmd.ExecuteScalar() == 0)
                {
                    var insertCmd = new SqlCommand(
                        "INSERT INTO Ayarlar (AyarAdi, AyarDegeri, Aciklama) VALUES (@ad, @deger, @aciklama)", conn);
                    insertCmd.Parameters.AddWithValue("@ad", ayar.Key);
                    insertCmd.Parameters.AddWithValue("@deger", ayar.Value.Deger);
                    insertCmd.Parameters.AddWithValue("@aciklama", ayar.Value.Aciklama);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
        
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
        
        public static (bool Success, string? Message, int? UserId, string? AdSoyad, string? Rol) VerifyLogin(string username, string password)
        {
            using var conn = GetConnection();
            conn.Open();
            
            var hash = HashPassword(password);
            var cmd = new SqlCommand(
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
        
        public static bool IsUsernameExists(string username)
        {
            using var conn = GetConnection();
            conn.Open();
            
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
            cmd.Parameters.AddWithValue("@user", username);
            return (int)cmd.ExecuteScalar() > 0;
        }
        
        public static string GetAyar(string ayarAdi)
        {
            using var conn = GetConnection();
            conn.Open();
            
            var cmd = new SqlCommand("SELECT AyarDegeri FROM Ayarlar WHERE AyarAdi = @ad", conn);
            cmd.Parameters.AddWithValue("@ad", ayarAdi);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
        
        public static void SetAyar(string ayarAdi, string ayarDegeri)
        {
            using var conn = GetConnection();
            conn.Open();
            
            var cmd = new SqlCommand("UPDATE Ayarlar SET AyarDegeri = @deger WHERE AyarAdi = @ad", conn);
            cmd.Parameters.AddWithValue("@ad", ayarAdi);
            cmd.Parameters.AddWithValue("@deger", ayarDegeri);
            cmd.ExecuteNonQuery();
        }
        
        public static decimal GetGecikmeUcreti()
        {
            var deger = GetAyar("GecikmeUcreti");
            return decimal.TryParse(deger.Replace(",", "."), System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var ucret) ? ucret : 1.00m;
        }
        
        public static int GetMaxOduncGun()
        {
            var deger = GetAyar("MaxOduncGun");
            return int.TryParse(deger, out var gun) ? gun : 14;
        }
    }
}
