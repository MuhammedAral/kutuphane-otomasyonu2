using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KutuphaneOtomasyon
{
    public class ApiService
    {
        private static readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        private static string _baseUrl = "https://kutuphane-api-production.up.railway.app";
        private static string? _token;
        
        static ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
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
            catch
            {
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
        
        public static async Task<List<UyeDto>?> GetUyelerAsync(string? search = null, string? rol = null)
        {
            try
            {
                var url = $"{_baseUrl}/api/uyeler";
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (!string.IsNullOrEmpty(rol)) queryParams.Add($"rol={Uri.EscapeDataString(rol)}");
                if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);
                
                return await _httpClient.GetFromJsonAsync<List<UyeDto>>(url);
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
        
        public static async Task<ApiResponse?> DeleteUyeAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/uyeler/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> CreateUyeAsync(UyeRequest uye)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/uyeler", uye);
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> UpdateUyeAsync(int id, UyeUpdateRequest uye)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/uyeler/{id}", uye);
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        // Ödünç için kitaplar (sadece mevcut olanlar)
        public static async Task<List<KitapDto>?> GetMevcutKitaplarAsync()
        {
            try
            {
                var kitaplar = await _httpClient.GetFromJsonAsync<List<KitapDto>>($"{_baseUrl}/api/kitaplar");
                return kitaplar?.Where(k => k.MevcutAdet > 0).ToList();
            }
            catch { return null; }
        }
        
        // Ödünç için üyeler (sadece Uye rolündekiler)
        public static async Task<List<UyeDto>?> GetUyelerForOduncAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UyeDto>>($"{_baseUrl}/api/uyeler?rol=Uye");
            }
            catch { return null; }
        }
        
        // Toplu kitap ekleme (Excel import için)
        public static async Task<TopluKitapResponse?> CreateKitaplarTopluAsync(List<KitapRequest> kitaplar)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/kitaplar/toplu", kitaplar);
                return await response.Content.ReadFromJsonAsync<TopluKitapResponse>();
            }
            catch { return null; }
        }
        
        // Toplu kitap silme
        public static async Task<ApiResponse?> DeleteKitaplarTopluAsync(List<int> kitapIds)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/kitaplar/toplu")
                {
                    Content = JsonContent.Create(kitapIds)
                };
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
            }
            catch { return null; }
        }
        
        // ISBN kontrolü
        public static async Task<ISBNCheckResponse?> CheckISBNAsync(string isbn)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ISBNCheckResponse>($"{_baseUrl}/api/kitaplar/isbn-kontrol?isbn={Uri.EscapeDataString(isbn)}");
            }
            catch { return null; }
        }
        
        // ==================== ODUNC ====================
        
        public static async Task<List<OduncDto>?> GetOdunclerAsync(string? filter = null, string? search = null)
        {
            try
            {
                var url = $"{_baseUrl}/api/odunc";
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(filter)) queryParams.Add($"filter={filter}");
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);
                
                return await _httpClient.GetFromJsonAsync<List<OduncDto>>(url);
            }
            catch { return null; }
        }
        
        public static async Task<OduncStatsDto?> GetOduncStatsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<OduncStatsDto>($"{_baseUrl}/api/odunc/stats");
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> CreateOduncAsync(int kitapId, int uyeId, int oduncGunu = 14)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/odunc", 
                    new { KitapID = kitapId, UyeID = uyeId, OduncGunu = oduncGunu });
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        public static async Task<IadeResponse?> IadeAsync(int islemId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{_baseUrl}/api/odunc/{islemId}/iade", null);
                return await response.Content.ReadFromJsonAsync<IadeResponse>();
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

        // ==================== DEĞERLENDİRMELER ====================
        
        public static async Task<ApiResponse?> DeleteDegerlendirmeAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/degerlendirmeler/{id}");
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
        }
        
        // ==================== RAPORLAR ====================
        
        public static async Task<RaporlarDto?> GetRaporlarAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<RaporlarDto>($"{_baseUrl}/api/raporlar");
            }
            catch { return null; }
        }
        
        public static void Logout()
        {
            Token = null;
        }
        
        // ==================== ÜYE PANELİ ====================
        
        public static async Task<UyeStatsDto?> GetUyeStatsAsync(int uyeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UyeStatsDto>($"{_baseUrl}/api/uye/{uyeId}/stats");
            }
            catch { return null; }
        }
        
        public static async Task<List<UyeSonIslemDto>?> GetUyeSonIslemlerAsync(int uyeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UyeSonIslemDto>>($"{_baseUrl}/api/uye/{uyeId}/son-islemler");
            }
            catch { return null; }
        }
        
        public static async Task<List<UyeOduncDto>?> GetUyeOdunclerAsync(int uyeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UyeOduncDto>>($"{_baseUrl}/api/uye/{uyeId}/oduncler");
            }
            catch { return null; }
        }
        
        public static async Task<UyeProfilDto?> GetUyeProfilAsync(int uyeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UyeProfilDto>($"{_baseUrl}/api/uye/{uyeId}/profil");
            }
            catch { return null; }
        }
        
        public static async Task<ApiResponse?> UpdateUyeProfilAsync(int uyeId, ProfilUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/uye/{uyeId}/profil", request);
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            catch { return null; }
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
        public string? Mesaj { get; set; }
        public bool Success { get; set; }
        public int? Id { get; set; }
    }
    
    public class UyeDto
    {
        public int KullaniciID { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string AdSoyad { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string Rol { get; set; } = "";
        public bool AktifMi { get; set; }
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
    
    public class OduncDto
    {
        public int IslemID { get; set; }
        public int KitapID { get; set; }
        public string Baslik { get; set; } = "";
        public int UyeID { get; set; }
        public string AdSoyad { get; set; } = "";
        public DateTime OduncTarihi { get; set; }
        public DateTime BeklenenIadeTarihi { get; set; }
        public DateTime? IadeTarihi { get; set; }
        public string Durum { get; set; } = "";
        public int GecikmeGun { get; set; }
        public decimal CezaMiktari { get; set; }
    }
    
    public class OduncStatsDto
    {
        public int AktifOdunc { get; set; }
        public int GecikenOdunc { get; set; }
        public decimal ToplamUcret { get; set; }
        public int BugunIade { get; set; }
        public decimal GecikmeUcreti { get; set; }
    }
    
    public class IadeResponse
    {
        public bool Success { get; set; }
        public string? Mesaj { get; set; }
        public int GecikmeGun { get; set; }
        public decimal CezaMiktari { get; set; }
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
    
    public class RaporlarDto
    {
        public int ToplamKitap { get; set; }
        public int ToplamUye { get; set; }
        public int AktifOdunc { get; set; }
        public int Geciken { get; set; }
        public int BuAyOdunc { get; set; }
        public int BuAyIade { get; set; }
        public int ToplamGecikmeGun { get; set; }
        public decimal ToplamGecikmeUcreti { get; set; }
        public List<TopKitapDto> TopKitaplar { get; set; } = new();
        public List<TopUyeDto> TopUyeler { get; set; } = new();
    }
    
    public class TopKitapDto
    {
        public int Sira { get; set; }
        public string Baslik { get; set; } = "";
        public int Sayi { get; set; }
    }
    
    public class TopUyeDto
    {
        public int Sira { get; set; }
        public string AdSoyad { get; set; } = "";
        public int Sayi { get; set; }
    }
    
    // Yeni Request ve Response sınıfları
    public class UyeRequest
    {
        public string KullaniciAdi { get; set; } = "";
        public string Sifre { get; set; } = "";
        public string AdSoyad { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
    }
    
    public class UyeUpdateRequest
    {
        public string AdSoyad { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
    }
    
    public class TopluKitapResponse
    {
        public bool Success { get; set; }
        public int Eklenen { get; set; }
        public int Hatali { get; set; }
        public List<string> Hatalar { get; set; } = new();
        public string? Mesaj { get; set; }
    }
    
    public class ISBNCheckResponse
    {
        public bool Exists { get; set; }
        public int? KitapID { get; set; }
        public string? Baslik { get; set; }
    }
    
    // Üye Paneli DTO'ları
    public class UyeStatsDto
    {
        public int Oduncte { get; set; }
        public int Geciken { get; set; }
        public int Toplam { get; set; }
    }
    
    public class UyeSonIslemDto
    {
        public string Baslik { get; set; } = "";
        public DateTime OduncTarihi { get; set; }
        public DateTime BeklenenIadeTarihi { get; set; }
        public string Durum { get; set; } = "";
    }
    
    public class UyeOduncDto
    {
        public string Baslik { get; set; } = "";
        public DateTime OduncTarihi { get; set; }
        public DateTime BeklenenIadeTarihi { get; set; }
        public string DurumText { get; set; } = "";
        public string Gecikme { get; set; } = "";
    }
    
    public class UyeProfilDto
    {
        public string AdSoyad { get; set; } = "";
        public string KullaniciAdi { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
    }
    
    public class ProfilUpdateRequest
    {
        public string AdSoyad { get; set; } = "";
        public string KullaniciAdi { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string MevcutSifre { get; set; } = "";
        public string? YeniSifre { get; set; }
    }
}
