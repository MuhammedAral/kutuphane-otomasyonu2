using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KutuphaneOtomasyon
{
    public class ApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _baseUrl = "http://localhost:5026";
        private static string? _token;
        
        public static string BaseUrl
        {
            get => _baseUrl;
            set => _baseUrl = value.TrimEnd('/');
        }
        
        public static string? Token
        {
            get => _token;
            set
            {
                _token = value;
                if (!string.IsNullOrEmpty(value))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
                else
                    _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
        
        public static bool IsLoggedIn => !string.IsNullOrEmpty(_token);
        
        // ==================== AUTH ====================
        
        public static async Task<LoginResponse?> LoginAsync(string kullaniciAdi, string sifre)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/giris", 
                    new { KullaniciAdi = kullaniciAdi, Sifre = sifre });
                    
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null)
                        Token = result.Token;
                    return result;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }
        
        public static async Task<ApiResponse?> RegisterAsync(string adSoyad, string email, string kullaniciAdi, string sifre)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/register", 
                    new { AdSoyad = adSoyad, Email = email, KullaniciAdi = kullaniciAdi, Sifre = sifre });
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> ForgotPasswordAsync(string email)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/sifremi-unuttum", 
                    new { Email = email });
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> ResetPasswordAsync(string email, string kod, string yeniSifre)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/sifre-sifirla", 
                    new { Email = email, Kod = kod, YeniSifre = yeniSifre });
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        // ==================== KITAPLAR ====================
        
        public static async Task<List<KitapDto>?> GetKitaplarAsync(string? search = null, int? turId = null)
        {
            try
            {
                var url = $"{_baseUrl}/api/kitaplar";
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (turId.HasValue) queryParams.Add($"turId={turId}");
                if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);
                
                return await _httpClient.GetFromJsonAsync<List<KitapDto>>(url);
            }
            catch { return null; }
        }
        
        public static async Task<KitapDto?> GetKitapAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<KitapDto>($"{_baseUrl}/api/kitaplar/{id}");
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> CreateKitapAsync(KitapRequest kitap)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/kitaplar", kitap);
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> UpdateKitapAsync(int id, KitapRequest kitap)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/kitaplar/{id}", kitap);
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> DeleteKitapAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/kitaplar/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        // ==================== UYELER ====================
        
        public static async Task<List<UyeDto>?> GetUyelerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UyeDto>>($"{_baseUrl}/api/uyeler");
            }
            catch { return null; }
        }
        
        public static async Task<UyeDto?> GetUyeAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UyeDto>($"{_baseUrl}/api/uyeler/{id}");
            }
            catch { return null; }
        }
        
        // ==================== ODUNC ====================
        
        public static async Task<List<OduncDto>?> GetOdunclerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<OduncDto>>($"{_baseUrl}/api/odunc");
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> CreateOduncAsync(int kitapId, int uyeId, int gun)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/odunc", 
                    new { KitapID = kitapId, UyeID = uyeId, Gun = gun });
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> IadeAsync(int islemId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{_baseUrl}/api/odunc/{islemId}/iade", null);
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        // ==================== TURLER ====================
        
        public static async Task<List<TurDto>?> GetTurlerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TurDto>>($"{_baseUrl}/api/turler");
            }
            catch { return null; }
        }
        
        // ==================== DASHBOARD ====================
        
        public static async Task<DashboardStatsDto?> GetDashboardStatsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DashboardStatsDto>($"{_baseUrl}/api/dashboard/stats");
            }
            catch { return null; }
        }
        
        public static async Task<List<GecikenKitapDto>?> GetGecikenKitaplarAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<GecikenKitapDto>>($"{_baseUrl}/api/dashboard/geciken-kitaplar");
            }
            catch { return null; }
        }
        
        public static void Logout()
        {
            Token = null;
        }
    }
    
    // ==================== DTO CLASSES ====================
    
    public class LoginResponse
    {
        public string? Token { get; set; }
        public int? UserId { get; set; }
        public string? AdSoyad { get; set; }
        public string? Rol { get; set; }
        public string? Mesaj { get; set; }
    }
    
    public class ApiResponse
    {
        public string? Message { get; set; }
        public bool Success { get; set; }
        public int? Id { get; set; }
    }
    
    public class KitapDto
    {
        public int KitapID { get; set; }
        public string Baslik { get; set; } = "";
        public string? Yazar { get; set; }
        public int? YayinYili { get; set; }
        public int StokAdedi { get; set; }
        public int MevcutAdet { get; set; }
        public string? ISBN { get; set; }
        public int? TurID { get; set; }
        public string? TurAdi { get; set; }
        public string? RafNo { get; set; }
        public string? SiraNo { get; set; }
        public string? Aciklama { get; set; }
    }
    
    public class KitapRequest
    {
        public string Baslik { get; set; } = "";
        public string? Yazar { get; set; }
        public int? YayinYili { get; set; }
        public int StokAdedi { get; set; }
        public string? ISBN { get; set; }
        public int? TurID { get; set; }
        public string? RafNo { get; set; }
        public string? SiraNo { get; set; }
        public string? Aciklama { get; set; }
    }
    
    public class UyeDto
    {
        public int KullaniciID { get; set; }
        public string AdSoyad { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string? KullaniciAdi { get; set; }
        public bool AktifMi { get; set; }
    }
    
    public class OduncDto
    {
        public int IslemID { get; set; }
        public int KitapID { get; set; }
        public string? KitapAdi { get; set; }
        public int UyeID { get; set; }
        public string? UyeAdi { get; set; }
        public DateTime OduncTarihi { get; set; }
        public DateTime SonTeslimTarihi { get; set; }
        public DateTime? IadeTarihi { get; set; }
    }
    
    public class TurDto
    {
        public int TurID { get; set; }
        public string TurAdi { get; set; } = "";
    }
    
    public class DashboardStatsDto
    {
        public int ToplamKitap { get; set; }
        public int ToplamUye { get; set; }
        public int AktifOdunc { get; set; }
        public int Gecikenler { get; set; }
    }
    
    public class GecikenKitapDto
    {
        public int IslemID { get; set; }
        public string AdSoyad { get; set; } = "";
        public string KitapBaslik { get; set; } = "";
        public DateTime BeklenenIadeTarihi { get; set; }
        public int GecikmeGun { get; set; }
    }
}
