using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace KutuphaneOtomasyon
{
    public static class DatabaseHelper
    {
        // Supabase PostgreSQL Connection String - Pooling ile optimize edilmiş
        private static readonly string ConnectionString = 
            "Host=db.cajuuwmwldceggretuyq.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=201005Ma.-;" +
            "SSL Mode=Require;Trust Server Certificate=true;" +
            "Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Idle Lifetime=300;Connection Pruning Interval=10";
        
        public static NpgsqlConnection GetConnection() => new(ConnectionString);
        
        public static void Initialize()
        {
            // Tablolar API tarafından oluşturulduğu için sadece bağlantıyı test et
            try
            {
                using var conn = GetConnection();
                conn.Open();
                Console.WriteLine("✅ Supabase bağlantısı başarılı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Veritabanı bağlantı hatası: {ex.Message}");
                throw;
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
            using var cmd = new NpgsqlCommand(
                "SELECT KullaniciID, AdSoyad, Rol FROM Kullanicilar WHERE KullaniciAdi = @user AND Sifre = @pass AND AktifMi = TRUE", conn);
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
            
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
            cmd.Parameters.AddWithValue("@user", username);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
        
        /// <summary>
        /// Ayar değerini getirir
        /// </summary>
        public static string GetAyar(string ayarAdi)
        {
            using var conn = GetConnection();
            conn.Open();
            
            using var cmd = new NpgsqlCommand("SELECT AyarDegeri FROM Ayarlar WHERE AyarAdi = @ad", conn);
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
            
            using var cmd = new NpgsqlCommand("UPDATE Ayarlar SET AyarDegeri = @deger WHERE AyarAdi = @ad", conn);
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
