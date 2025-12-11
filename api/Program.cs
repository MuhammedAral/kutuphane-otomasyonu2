using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KutuphaneApi.Services;

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
builder.Services.AddTransient<IEmailService, EmailService>();

var app = builder.Build();

// Configure
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication(); // Kimlik Doğrulama (Önce bu)
app.UseAuthorization();  // Yetkilendirme (Sonra bu)

// Static files - website klasöründen
var websitePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "website");
if (Directory.Exists(websitePath))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(websitePath)
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(websitePath),
        RequestPath = ""
    });
}

// Database connection string
var connectionString = "Server=tcp:127.0.0.1,1433;Database=KutuphaneDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True;";

// Yardımcı Metot: Şifre Hashleme
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes).ToLower();
}



// Uygulama Başlarken Tabloyu Kontrol Et
using (var scope = app.Services.CreateScope())
{
    try 
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        var tableCmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SifreSifirlamaIslemleri' and xtype='U')
            BEGIN
                CREATE TABLE SifreSifirlamaIslemleri (
                    IslemID INT PRIMARY KEY IDENTITY(1,1),
                    KullaniciID INT NOT NULL,
                    Kod NVARCHAR(10) NOT NULL,
                    OlusturmaTarihi DATETIME DEFAULT GETDATE(),
                    SonKullanmaTarihi DATETIME NOT NULL,
                    KullanildiMi BIT DEFAULT 0,
                    FOREIGN KEY (KullaniciID) REFERENCES Kullanicilar(KullaniciID)
                )
            END", conn);
        tableCmd.ExecuteNonQuery();

        // Eksik Kolon Kontrolü (Migration)
        // 1. AktifMi
        var colCmd = new SqlCommand("SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Kullanicilar') AND name = 'AktifMi'", conn);
        if ((int)colCmd.ExecuteScalar() == 0)
        {
            new SqlCommand("ALTER TABLE Kullanicilar ADD AktifMi BIT DEFAULT 1", conn).ExecuteNonQuery();
        }

        // 2. Telefon
        colCmd = new SqlCommand("SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Kullanicilar') AND name = 'Telefon'", conn);
        if ((int)colCmd.ExecuteScalar() == 0)
        {
            new SqlCommand("ALTER TABLE Kullanicilar ADD Telefon NVARCHAR(20)", conn).ExecuteNonQuery();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB Başlangıç hatası: " + ex.Message);
    }
}

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

.AllowAnonymous();

// ==================== ŞİFRE SIFIRLAMA API ====================

app.MapPost("/api/auth/sifremi-unuttum", async (ForgotPasswordRequest request, IEmailService emailService) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    // 1. Kullanıcı var mı kontrol et
    var cmd = new SqlCommand("SELECT KullaniciID, AdSoyad FROM Kullanicilar WHERE Email = @email AND AktifMi = 1", conn);
    cmd.Parameters.AddWithValue("@email", request.Email);
    
    int? userId = null;
    string adSoyad = "";
    
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.Read())
        {
            userId = reader.GetInt32(0);
            adSoyad = reader.GetString(1);
        }
    }

    if (userId == null)
        return Results.NotFound(new { message = "Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı." });

    // 2. Rastgele Kod Oluştur (6 haneli)
    var random = new Random();
    var kod = random.Next(100000, 999999).ToString();
    var sonKullanma = DateTime.Now.AddMinutes(15); // 15 dakika geçerli

    // 3. Kodu DB'ye kaydet
    var insertCmd = new SqlCommand(@"
        INSERT INTO SifreSifirlamaIslemleri (KullaniciID, Kod, SonKullanmaTarihi, KullanildiMi)
        VALUES (@uid, @kod, @skk, 0)", conn);
    insertCmd.Parameters.AddWithValue("@uid", userId);
    insertCmd.Parameters.AddWithValue("@kod", kod);
    insertCmd.Parameters.AddWithValue("@skk", sonKullanma);
    await insertCmd.ExecuteNonQueryAsync();

    // 4. Mail Gönder
    try
    {
        await emailService.SendEmailAsync(request.Email, "Şifre Sıfırlama Kodu", 
            $"Sayın {adSoyad},<br><br>Şifre sıfırlama talebiniz alınmıştır.<br>Doğrulama Kodunuz: <b>{kod}</b><br><br>Bu kod 15 dakika geçerlidir.");
        return Results.Ok(new { message = "Doğrulama kodu e-posta adresinize gönderildi." });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Email gönderilemedi: {ex.Message}");
    }
})
.WithName("SifremiUnuttum")
.WithTags("Giriş")
.AllowAnonymous();

app.MapPost("/api/auth/sifre-sifirla", async (ResetPasswordRequest request) =>
{
    if (request.YeniSifre.Length < 6)
        return Results.BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });

    using var conn = new SqlConnection(connectionString);
    conn.Open();

    // 1. Kodu doğrula
    var cmd = new SqlCommand(@"
        SELECT TOP 1 IslemID, KullaniciID FROM SifreSifirlamaIslemleri 
        WHERE Kod = @kod AND KullanildiMi = 0 AND SonKullanmaTarihi > GETDATE()
        ORDER BY IslemID DESC", conn);
    cmd.Parameters.AddWithValue("@kod", request.Kod);

    int islemId = 0;
    int userId = 0;

    using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.Read())
        {
            islemId = reader.GetInt32(0);
            userId = reader.GetInt32(1);
        }
    }

    if (islemId == 0)
        return Results.BadRequest(new { message = "Geçersiz veya süresi dolmuş kod." });

    // 2. Şifreyi Güncelle
    var hash = HashPassword(request.YeniSifre);
    var updateCmd = new SqlCommand("UPDATE Kullanicilar SET Sifre = @pass WHERE KullaniciID = @uid", conn);
    updateCmd.Parameters.AddWithValue("@pass", hash);
    updateCmd.Parameters.AddWithValue("@uid", userId);
    await updateCmd.ExecuteNonQueryAsync();

    // 3. Kodu kullanıldı olarak işaretle
    var expireCmd = new SqlCommand("UPDATE SifreSifirlamaIslemleri SET KullanildiMi = 1 WHERE IslemID = @iid", conn);
    expireCmd.Parameters.AddWithValue("@iid", islemId);
    await expireCmd.ExecuteNonQueryAsync();

    return Results.Ok(new { message = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz." });
})
.WithName("SifreSifirla")
.WithTags("Giriş")
.AllowAnonymous();

app.MapPost("/api/auth/register", async (RegisterRequest request, IEmailService emailService) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    // 1. Validasyonlar
    if (!request.Email.EndsWith("@gmail.com"))
        return Results.BadRequest(new { message = "Sadece @gmail.com uzantılı mail adresleri kabul edilmektedir." });

    if (request.Sifre.Length < 6)
        return Results.BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });

    // 2. Kullanıcı Adı veya Email Kontrolü
    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user OR Email = @email", conn);
    checkCmd.Parameters.AddWithValue("@user", request.KullaniciAdi);
    checkCmd.Parameters.AddWithValue("@email", request.Email);
    if ((int)await checkCmd.ExecuteScalarAsync() > 0)
        return Results.BadRequest(new { message = "Bu kullanıcı adı veya e-posta adresi zaten kullanılıyor." });

    // 3. Kullanıcıyı Pasif Olarak Ekle
    var hash = HashPassword(request.Sifre);
    var insertCmd = new SqlCommand(@"
        INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol, AktifMi) 
        OUTPUT INSERTED.KullaniciID
        VALUES (@user, @pass, @ad, @email, @tel, 'Uye', 0)", conn);
    
    insertCmd.Parameters.AddWithValue("@user", request.KullaniciAdi);
    insertCmd.Parameters.AddWithValue("@pass", hash);
    insertCmd.Parameters.AddWithValue("@ad", request.AdSoyad);
    insertCmd.Parameters.AddWithValue("@email", request.Email);
    insertCmd.Parameters.AddWithValue("@tel", (object?)request.Telefon ?? DBNull.Value);
    
    var userId = (int)await insertCmd.ExecuteScalarAsync();

    // 4. Doğrulama Kodu Oluştur ve Gönder
    var random = new Random();
    var kod = random.Next(100000, 999999).ToString();
    var sonKullanma = DateTime.Now.AddMinutes(15); 

    var codeCmd = new SqlCommand(@"
        INSERT INTO SifreSifirlamaIslemleri (KullaniciID, Kod, SonKullanmaTarihi, KullanildiMi)
        VALUES (@uid, @kod, @skk, 0)", conn); // Aynı tabloyu doğrulama için de kullanıyoruz
    codeCmd.Parameters.AddWithValue("@uid", userId);
    codeCmd.Parameters.AddWithValue("@kod", kod);
    codeCmd.Parameters.AddWithValue("@skk", sonKullanma);
    await codeCmd.ExecuteNonQueryAsync();

    try {
        await emailService.SendEmailAsync(request.Email, "Kütüphane Üyelik Doğrulama", 
            $"Merhaba {request.AdSoyad},<br><br>Kayıt işlemini tamamlamak için doğrulama kodunuz: <b>{kod}</b><br><br>Bu kod 15 dakika geçerlidir.");
    } catch {}

    return Results.Ok(new { message = "Kayıt başarılı. Lütfen e-mail adresinize gelen kodu giriniz.", userId = userId });
})
.WithName("Register")
.WithTags("Giriş")
.AllowAnonymous();

app.MapPost("/api/auth/verify-email", async (VerifyRequest request) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var cmd = new SqlCommand(@"
        SELECT TOP 1 IslemID, KullaniciID FROM SifreSifirlamaIslemleri 
        WHERE Kod = @kod AND KullaniciID = @uid AND KullanildiMi = 0 AND SonKullanmaTarihi > GETDATE()
        ORDER BY IslemID DESC", conn);
    cmd.Parameters.AddWithValue("@kod", request.Kod);
    cmd.Parameters.AddWithValue("@uid", request.UserId);

    int islemId = 0;
    
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.Read()) islemId = reader.GetInt32(0);
    }

    if (islemId == 0) return Results.BadRequest(new { message = "Geçersiz veya süresi dolmuş kod." });

    // Hesabı Aktifleştir
    await new SqlCommand($"UPDATE Kullanicilar SET AktifMi = 1 WHERE KullaniciID = {request.UserId}", conn).ExecuteNonQueryAsync();
    
    // Kodu yak
    await new SqlCommand($"UPDATE SifreSifirlamaIslemleri SET KullanildiMi = 1 WHERE IslemID = {islemId}", conn).ExecuteNonQueryAsync();

    return Results.Ok(new { message = "Hesabınız başarıyla doğrulandı. Giriş yapabilirsiniz." });
})
.WithName("VerifyEmail")
.WithTags("Giriş")
.AllowAnonymous();

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

app.MapPost("/api/odunc", async (OduncRequest request, IEmailService emailService) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    // Kullanıcı bilgisi ve email kontrolü
    string? emailAdresi = null;
    string adSoyad = "";
    
    using (var userCmd = new SqlCommand("SELECT Email, AdSoyad FROM Kullanicilar WHERE KullaniciID = @uye", conn))
    {
        userCmd.Parameters.AddWithValue("@uye", request.UyeID);
        using var reader = await userCmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            emailAdresi = reader.IsDBNull(0) ? null : reader.GetString(0);
            adSoyad = reader.GetString(1);
        }
    } // Reader kapatıldı

    var cmd = new SqlCommand(@"
        INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi) 
        OUTPUT INSERTED.IslemID
        VALUES (@kitap, @uye, DATEADD(DAY, @gun, GETDATE()));
        UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = @kitap", conn);
    
    cmd.Parameters.AddWithValue("@kitap", request.KitapID);
    cmd.Parameters.AddWithValue("@uye", request.UyeID);
    cmd.Parameters.AddWithValue("@gun", request.OduncGunu ?? 14);
    
    var id = await cmd.ExecuteScalarAsync();

    // Email Gönderimi
    if (!string.IsNullOrEmpty(emailAdresi))
    {
        try 
        {
            string kitapAdi = "";
            using (var kCmd = new SqlCommand("SELECT Baslik FROM Kitaplar WHERE KitapID = @id", conn))
            {
                kCmd.Parameters.AddWithValue("@id", request.KitapID);
                var result = await kCmd.ExecuteScalarAsync();
                if (result != null) kitapAdi = result.ToString()!;
            }

            await emailService.SendEmailAsync(emailAdresi, "Kitap Ödünç Alma Bildirimi", 
                $"Sayın {adSoyad},<br><br>'{kitapAdi}' adlı kitabı kütüphanemizden ödünç aldınız.<br>İade tarihiniz: {DateTime.Now.AddDays(request.OduncGunu ?? 14):dd.MM.yyyy}.<br><br>İyi okumalar!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email gönderme hatası: {ex.Message}");
        }
    }

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

// Üyeye özel ödünç listesi
app.MapGet("/api/odunc/uye/{uyeId}", (int uyeId) =>
{
    var islemler = new List<object>();
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    var cmd = new SqlCommand(@"
        SELECT o.IslemID, k.Baslik, k.Yazar, o.OduncTarihi, o.BeklenenIadeTarihi, o.IadeTarihi, o.Durum,
            CASE 
                WHEN o.Durum = 'Odunc' AND o.BeklenenIadeTarihi < GETDATE() 
                THEN DATEDIFF(DAY, o.BeklenenIadeTarihi, GETDATE())
                ELSE 0 
            END as GecikmeGun
        FROM OduncIslemleri o
        JOIN Kitaplar k ON o.KitapID = k.KitapID
        WHERE o.UyeID = @uyeId
        ORDER BY o.IslemID DESC", conn);
    cmd.Parameters.AddWithValue("@uyeId", uyeId);
    
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        islemler.Add(new
        {
            IslemID = reader.GetInt32(0),
            Baslik = reader.GetString(1),
            Yazar = reader.GetString(2),
            OduncTarihi = reader.GetDateTime(3),
            BeklenenIadeTarihi = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
            IadeTarihi = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
            Durum = reader.GetString(6),
            GecikmeGun = reader.GetInt32(7)
        });
    }
    return Results.Ok(islemler);
})
.WithName("GetUyeOdunc")
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
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Kod, string YeniSifre);
public record RegisterRequest(string KullaniciAdi, string Sifre, string AdSoyad, string Email, string? Telefon);
public record VerifyRequest(int UserId, string Kod);
