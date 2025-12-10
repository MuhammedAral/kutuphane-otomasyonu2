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
                );";
            
            new SqlCommand(sql, conn).ExecuteNonQuery();
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
    }
}
