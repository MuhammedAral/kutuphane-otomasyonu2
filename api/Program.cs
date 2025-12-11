using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();

// Swagger Konfigürasyonu (Yetkilendirme Desteği ile)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Kütüphane API", Version = "v1" });
    
    // Swagger'da kilit simgesi ekle
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Örnek: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// JWT Authentication Ayarları
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication(); // Kimlik Doğrulama (Önce bu)
app.UseAuthorization();  // Yetkilendirme (Sonra bu)

// Database connection string
var connectionString = "Server=localhost;Database=KutuphaneDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True;";

// Yardımcı Metot: Şifre Hashleme
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes).ToLower();
}

// ==================== GİRİŞ API (TOKEN ALMA) ====================

app.MapPost("/api/giris", (LoginRequest request) =>
{
    if (string.IsNullOrEmpty(request.KullaniciAdi) || string.IsNullOrEmpty(request.Sifre))
        return Results.BadRequest("Kullanıcı adı ve şifre gereklidir.");

    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var hash = HashPassword(request.Sifre);
    var cmd = new SqlCommand("SELECT KullaniciID, AdSoyad, Rol FROM Kullanicilar WHERE KullaniciAdi = @user AND Sifre = @pass AND AktifMi = 1", conn);
    cmd.Parameters.AddWithValue("@user", request.KullaniciAdi);
    cmd.Parameters.AddWithValue("@pass", hash);

    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        var uid = reader.GetInt32(0);
        var adSoyad = reader.GetString(1);
        var rol = reader.GetString(2);

        // Token Oluşturma
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, uid.ToString()),
                new Claim(ClaimTypes.Name, adSoyad),
                new Claim(ClaimTypes.Role, rol)
            }),
            Expires = DateTime.UtcNow.AddHours(2), // Token 2 saat geçerli
            Issuer = builder.Configuration["Jwt:Issuer"],
            Audience = builder.Configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Results.Ok(new { Token = tokenHandler.WriteToken(token), Mesaj = "Giriş Başarılı" });
    }

    return Results.Unauthorized();
})
.WithName("GirisYap")
.WithTags("Giriş")
.AllowAnonymous(); // Giriş herkese açık olmalı

// ==================== KİTAPLAR API ====================

app.MapGet("/api/kitaplar", () =>
{
    var kitaplar = new List<object>();
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        SELECT k.KitapID, k.Baslik, k.Yazar, ISNULL(k.ISBN, '') as ISBN, 
               ISNULL(kt.TurAdi, '-') as TurAdi, k.StokAdedi, k.MevcutAdet, ISNULL(k.RafNo, '') as RafNo
        FROM Kitaplar k
        LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
        ORDER BY k.KitapID DESC", conn);
    
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        kitaplar.Add(new
        {
            KitapID = reader.GetInt32(0),
            Baslik = reader.GetString(1),
            Yazar = reader.GetString(2),
            ISBN = reader.GetString(3),
            TurAdi = reader.GetString(4),
            StokAdedi = reader.GetInt32(5),
            MevcutAdet = reader.GetInt32(6),
            RafNo = reader.GetString(7)
        });
    }
    return Results.Ok(kitaplar);
})
.WithName("GetKitaplar")
.WithTags("Kitaplar")
.RequireAuthorization(); // ARTIK KİLİTLİ!

app.MapGet("/api/kitaplar/{id}", (int id) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand("SELECT * FROM Kitaplar WHERE KitapID = @id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        return Results.Ok(new
        {
            KitapID = reader["KitapID"],
            Baslik = reader["Baslik"],
            Yazar = reader["Yazar"],
            ISBN = reader["ISBN"],
            YayinYili = reader["YayinYili"],
            TurID = reader["TurID"],
            StokAdedi = reader["StokAdedi"],
            MevcutAdet = reader["MevcutAdet"],
            RafNo = reader["RafNo"]
        });
    }
    return Results.NotFound(new { message = "Kitap bulunamadı" });
})
.WithName("GetKitap")
.WithTags("Kitaplar")
.RequireAuthorization();

app.MapPost("/api/kitaplar", (KitapRequest kitap) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        INSERT INTO Kitaplar (Baslik, Yazar, ISBN, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo)
        OUTPUT INSERTED.KitapID
        VALUES (@baslik, @yazar, @isbn, @yil, @tur, @stok, @stok, @raf)", conn);
    
    cmd.Parameters.AddWithValue("@baslik", kitap.Baslik);
    cmd.Parameters.AddWithValue("@yazar", kitap.Yazar);
    cmd.Parameters.AddWithValue("@isbn", (object?)kitap.ISBN ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@yil", (object?)kitap.YayinYili ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@tur", (object?)kitap.TurID ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@stok", kitap.StokAdedi ?? 1);
    cmd.Parameters.AddWithValue("@raf", (object?)kitap.RafNo ?? DBNull.Value);
    
    var id = cmd.ExecuteScalar();
    return Results.Created($"/api/kitaplar/{id}", new { KitapID = id, message = "Kitap eklendi" });
})
.WithName("CreateKitap")
.WithTags("Kitaplar")
.RequireAuthorization();

app.MapPut("/api/kitaplar/{id}", (int id, KitapRequest kitap) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        UPDATE Kitaplar SET Baslik = @baslik, Yazar = @yazar, ISBN = @isbn, 
        YayinYili = @yil, TurID = @tur, StokAdedi = @stok, RafNo = @raf
        WHERE KitapID = @id", conn);
    
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@baslik", kitap.Baslik);
    cmd.Parameters.AddWithValue("@yazar", kitap.Yazar);
    cmd.Parameters.AddWithValue("@isbn", (object?)kitap.ISBN ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@yil", (object?)kitap.YayinYili ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@tur", (object?)kitap.TurID ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@stok", kitap.StokAdedi ?? 1);
    cmd.Parameters.AddWithValue("@raf", (object?)kitap.RafNo ?? DBNull.Value);
    
    var affected = cmd.ExecuteNonQuery();
    return affected > 0 ? Results.Ok(new { message = "Kitap güncellendi" }) : Results.NotFound();
})
.WithName("UpdateKitap")
.WithTags("Kitaplar")
.RequireAuthorization();

app.MapDelete("/api/kitaplar/{id}", (int id) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand("DELETE FROM Kitaplar WHERE KitapID = @id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    var affected = cmd.ExecuteNonQuery();
    return affected > 0 ? Results.Ok(new { message = "Kitap silindi" }) : Results.NotFound();
})
.WithName("DeleteKitap")
.WithTags("Kitaplar")
.RequireAuthorization();

// ==================== ÜYELER API ====================

app.MapGet("/api/uyeler", () =>
{
    var uyeler = new List<object>();
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        SELECT KullaniciID, KullaniciAdi, AdSoyad, ISNULL(Email, '') as Email, 
               ISNULL(Telefon, '') as Telefon, Rol, AktifMi
        FROM Kullanicilar ORDER BY KullaniciID DESC", conn);
    
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        uyeler.Add(new
        {
            KullaniciID = reader.GetInt32(0),
            KullaniciAdi = reader.GetString(1),
            AdSoyad = reader.GetString(2),
            Email = reader.GetString(3),
            Telefon = reader.GetString(4),
            Rol = reader.GetString(5),
            AktifMi = reader.GetBoolean(6)
        });
    }
    return Results.Ok(uyeler);
})
.WithName("GetUyeler")
.WithTags("Üyeler")
.RequireAuthorization();

app.MapGet("/api/uyeler/{id}", (int id) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand("SELECT * FROM Kullanicilar WHERE KullaniciID = @id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        return Results.Ok(new
        {
            KullaniciID = reader["KullaniciID"],
            KullaniciAdi = reader["KullaniciAdi"],
            AdSoyad = reader["AdSoyad"],
            Email = reader["Email"],
            Telefon = reader["Telefon"],
            Rol = reader["Rol"],
            AktifMi = reader["AktifMi"]
        });
    }
    return Results.NotFound(new { message = "Üye bulunamadı" });
})
.WithName("GetUye")
.WithTags("Üyeler")
.RequireAuthorization();

// ==================== ÖDÜNÇ İŞLEMLERİ API ====================

app.MapGet("/api/odunc", () =>
{
    var islemler = new List<object>();
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        SELECT o.IslemID, k.Baslik, u.AdSoyad, o.OduncTarihi, o.BeklenenIadeTarihi, o.IadeTarihi, o.Durum
        FROM OduncIslemleri o
        JOIN Kitaplar k ON o.KitapID = k.KitapID
        JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
        ORDER BY o.IslemID DESC", conn);
    
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        islemler.Add(new
        {
            IslemID = reader.GetInt32(0),
            Baslik = reader.GetString(1),
            AdSoyad = reader.GetString(2),
            OduncTarihi = reader.GetDateTime(3),
            BeklenenIadeTarihi = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
            IadeTarihi = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
            Durum = reader.GetString(6)
        });
    }
    return Results.Ok(islemler);
})
.WithName("GetOdunc")
.WithTags("Ödünç İşlemleri")
.RequireAuthorization();

app.MapPost("/api/odunc", (OduncRequest request) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi) 
        OUTPUT INSERTED.IslemID
        VALUES (@kitap, @uye, DATEADD(DAY, @gun, GETDATE()));
        UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = @kitap", conn);
    
    cmd.Parameters.AddWithValue("@kitap", request.KitapID);
    cmd.Parameters.AddWithValue("@uye", request.UyeID);
    cmd.Parameters.AddWithValue("@gun", request.OduncGunu ?? 14);
    
    var id = cmd.ExecuteScalar();
    return Results.Created($"/api/odunc/{id}", new { IslemID = id, message = "Ödünç verildi" });
})
.WithName("CreateOdunc")
.WithTags("Ödünç İşlemleri")
.RequireAuthorization();

app.MapPut("/api/odunc/{id}/iade", (int id) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        UPDATE OduncIslemleri SET Durum = 'IadeEdildi', IadeTarihi = GETDATE() WHERE IslemID = @id;
        UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 
        WHERE KitapID = (SELECT KitapID FROM OduncIslemleri WHERE IslemID = @id)", conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    var affected = cmd.ExecuteNonQuery();
    return affected > 0 ? Results.Ok(new { message = "Kitap iade alındı" }) : Results.NotFound();
})
.WithName("IadeAl")
.WithTags("Ödünç İşlemleri")
.RequireAuthorization();

// ==================== KİTAP TÜRLERİ API ====================

app.MapGet("/api/turler", () =>
{
    var turler = new List<object>();
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand("SELECT TurID, TurAdi FROM KitapTurleri", conn);
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        turler.Add(new { TurID = reader.GetInt32(0), TurAdi = reader.GetString(1) });
    }
    return Results.Ok(turler);
})
.WithName("GetTurler")
.WithTags("Kitap Türleri")
.RequireAuthorization();

// ==================== İSTATİSTİKLER API ====================

app.MapGet("/api/istatistikler", () =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var kitapSayisi = new SqlCommand("SELECT COUNT(*) FROM Kitaplar", conn).ExecuteScalar();
    var uyeSayisi = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE Rol = 'Uye'", conn).ExecuteScalar();
    var oduncte = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE Durum = 'Odunc'", conn).ExecuteScalar();
    var geciken = new SqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE Durum = 'Odunc' AND BeklenenIadeTarihi < GETDATE()", conn).ExecuteScalar();
    
    return Results.Ok(new
    {
        ToplamKitap = kitapSayisi,
        ToplamUye = uyeSayisi,
        OduncteKitap = oduncte,
        GecikenKitap = geciken
    });
})
.WithName("GetIstatistikler")
.WithTags("İstatistikler")
.RequireAuthorization();

app.Run();

// ==================== REQUEST MODELS ====================

public record LoginRequest(string KullaniciAdi, string Sifre);
public record KitapRequest(string Baslik, string Yazar, string? ISBN, int? YayinYili, int? TurID, int? StokAdedi, string? RafNo);
public record OduncRequest(int KitapID, int UyeID, int? OduncGunu);
