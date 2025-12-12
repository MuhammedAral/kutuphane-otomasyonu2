using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;
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

// Database connection string - SUPABASE PostgreSQL
var connectionString = "Host=db.cajuuwmwldceggretuyq.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=201005Ma.-;SSL Mode=Require;Trust Server Certificate=true";

// Yardımcı Metot: Şifre Hashleme
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes).ToLower();
}

// Uygulama Başlarken Tabloları Oluştur (PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    try 
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        
        // Eski büyük harfli tabloları sil (varsa)
        var dropOldTables = new NpgsqlCommand(@"
            DROP TABLE IF EXISTS ""SifreSifirlamaIslemleri"" CASCADE;
            DROP TABLE IF EXISTS ""Degerlendirmeler"" CASCADE;
            DROP TABLE IF EXISTS ""OduncIslemleri"" CASCADE;
            DROP TABLE IF EXISTS ""Kitaplar"" CASCADE;
            DROP TABLE IF EXISTS ""KitapTurleri"" CASCADE;
            DROP TABLE IF EXISTS ""Ayarlar"" CASCADE;
            DROP TABLE IF EXISTS ""Kullanicilar"" CASCADE;
        ", conn);
        dropOldTables.ExecuteNonQuery();
        
        // Tabloları oluştur (PostgreSQL syntax - lowercase)
        var createTablesCmd = new NpgsqlCommand(@"
            -- Kullanicilar tablosu
            CREATE TABLE IF NOT EXISTS Kullanicilar (
                KullaniciID SERIAL PRIMARY KEY,
                KullaniciAdi VARCHAR(50) UNIQUE NOT NULL,
                Sifre VARCHAR(256) NOT NULL,
                AdSoyad VARCHAR(100) NOT NULL,
                Email VARCHAR(100),
                Telefon VARCHAR(20),
                Rol VARCHAR(20) DEFAULT 'Uye',
                AktifMi BOOLEAN DEFAULT TRUE,
                OlusturmaTarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            -- KitapTurleri tablosu
            CREATE TABLE IF NOT EXISTS KitapTurleri (
                TurID SERIAL PRIMARY KEY,
                TurAdi VARCHAR(50) NOT NULL
            );

            -- Kitaplar tablosu
            CREATE TABLE IF NOT EXISTS Kitaplar (
                KitapID SERIAL PRIMARY KEY,
                Baslik VARCHAR(200) NOT NULL,
                Yazar VARCHAR(100) NOT NULL,
                ISBN VARCHAR(20),
                Barkod VARCHAR(50),
                YayinYili INTEGER,
                TurID INTEGER REFERENCES KitapTurleri(TurID),
                StokAdedi INTEGER DEFAULT 1,
                MevcutAdet INTEGER DEFAULT 1,
                RafNo VARCHAR(20),
                SiraNo VARCHAR(20),
                Aciklama VARCHAR(500),
                EklenmeTarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            -- OduncIslemleri tablosu
            CREATE TABLE IF NOT EXISTS OduncIslemleri (
                IslemID SERIAL PRIMARY KEY,
                KitapID INTEGER REFERENCES Kitaplar(KitapID),
                UyeID INTEGER REFERENCES Kullanicilar(KullaniciID),
                OduncTarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                BeklenenIadeTarihi TIMESTAMP,
                IadeTarihi TIMESTAMP,
                Durum VARCHAR(20) DEFAULT 'Odunc',
                CezaMiktari DECIMAL(10,2) DEFAULT 0
            );

            -- Ayarlar tablosu
            CREATE TABLE IF NOT EXISTS Ayarlar (
                AyarID SERIAL PRIMARY KEY,
                AyarAdi VARCHAR(50) UNIQUE NOT NULL,
                AyarDegeri VARCHAR(100) NOT NULL,
                Aciklama VARCHAR(200)
            );

            -- Degerlendirmeler tablosu
            CREATE TABLE IF NOT EXISTS Degerlendirmeler (
                DegerlendirmeID SERIAL PRIMARY KEY,
                UyeID INTEGER REFERENCES Kullanicilar(KullaniciID),
                KitapID INTEGER REFERENCES Kitaplar(KitapID),
                Puan SMALLINT CHECK (Puan >= 1 AND Puan <= 5),
                Yorum VARCHAR(500),
                Tarih TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            -- SifreSifirlamaIslemleri tablosu
            CREATE TABLE IF NOT EXISTS SifreSifirlamaIslemleri (
                IslemID SERIAL PRIMARY KEY,
                KullaniciID INTEGER NOT NULL REFERENCES Kullanicilar(KullaniciID),
                Kod VARCHAR(10) NOT NULL,
                OlusturmaTarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                SonKullanmaTarihi TIMESTAMP NOT NULL,
                KullanildiMi BOOLEAN DEFAULT FALSE
            );
        ", conn);
        createTablesCmd.ExecuteNonQuery();
        
        // Varsayılan admin kontrolü ve ekleme
        var checkAdmin = new NpgsqlCommand(@"SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user", conn);
        checkAdmin.Parameters.AddWithValue("@user", "admin");
        if (Convert.ToInt32(checkAdmin.ExecuteScalar()) == 0)
        {
            var hash = HashPassword("admin123");
            var insertAdmin = new NpgsqlCommand(@"
                INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Rol, AktifMi) 
                VALUES (@user, @sifre, @ad, @rol, TRUE)", conn);
            insertAdmin.Parameters.AddWithValue("@user", "admin");
            insertAdmin.Parameters.AddWithValue("@sifre", hash);
            insertAdmin.Parameters.AddWithValue("@ad", "Sistem Yöneticisi");
            insertAdmin.Parameters.AddWithValue("@rol", "Yonetici");
            insertAdmin.ExecuteNonQuery();
        }
        
        // Varsayılan kitap türleri
        var turler = new[] { "Roman", "Hikaye", "Şiir", "Tarih", "Bilim", "Felsefe", "Çocuk", "Eğitim" };
        foreach (var tur in turler)
        {
            var checkTur = new NpgsqlCommand(@"SELECT COUNT(*) FROM KitapTurleri WHERE TurAdi = @tur", conn);
            checkTur.Parameters.AddWithValue("@tur", tur);
            if (Convert.ToInt32(checkTur.ExecuteScalar()) == 0)
            {
                var insertTur = new NpgsqlCommand(@"INSERT INTO KitapTurleri (TurAdi) VALUES (@tur)", conn);
                insertTur.Parameters.AddWithValue("@tur", tur);
                insertTur.ExecuteNonQuery();
            }
        }
        
        // Varsayılan ayarlar
        var ayarlar = new Dictionary<string, (string Deger, string Aciklama)>
        {
            { "GecikmeUcreti", ("1.00", "Gün başına gecikme ücreti (TL)") },
            { "MaxOduncGun", ("14", "Maksimum ödünç verme süresi (gün)") }
        };
        foreach (var ayar in ayarlar)
        {
            var checkAyar = new NpgsqlCommand(@"SELECT COUNT(*) FROM Ayarlar WHERE AyarAdi = @ad", conn);
            checkAyar.Parameters.AddWithValue("@ad", ayar.Key);
            if (Convert.ToInt32(checkAyar.ExecuteScalar()) == 0)
            {
                var insertAyar = new NpgsqlCommand(@"
                    INSERT INTO Ayarlar (AyarAdi, AyarDegeri, Aciklama) 
                    VALUES (@ad, @deger, @aciklama)", conn);
                insertAyar.Parameters.AddWithValue("@ad", ayar.Key);
                insertAyar.Parameters.AddWithValue("@deger", ayar.Value.Deger);
                insertAyar.Parameters.AddWithValue("@aciklama", ayar.Value.Aciklama);
                insertAyar.ExecuteNonQuery();
            }
        }
        
        Console.WriteLine("✅ Supabase PostgreSQL bağlantısı başarılı! Tablolar oluşturuldu.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ DB Başlangıç hatası: " + ex.Message);
    }
}

// ==================== GİRİŞ API ====================

app.MapPost("/api/giris", (LoginRequest request) =>
{
    if (string.IsNullOrEmpty(request.KullaniciAdi) || string.IsNullOrEmpty(request.Sifre))
        return Results.BadRequest("Kullanıcı adı ve şifre gereklidir.");

    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();

    var hash = HashPassword(request.Sifre);
    var cmd = new NpgsqlCommand(@"SELECT KullaniciID, AdSoyad, Rol FROM Kullanicilar WHERE KullaniciAdi = @user AND Sifre = @pass AND AktifMi = TRUE", conn);
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
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = builder.Configuration["Jwt:Issuer"],
            Audience = builder.Configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        // Kullanıcı bilgilerini de döndür
        return Results.Ok(new { 
            Token = tokenHandler.WriteToken(token), 
            UserId = uid,
            AdSoyad = adSoyad,
            Rol = rol,
            Mesaj = "Giriş Başarılı" 
        });
    }

    return Results.Unauthorized();
})
.WithName("GirisYap")
.WithTags("Giriş")
.AllowAnonymous();

// ==================== DASHBOARD API ====================

app.MapGet("/api/dashboard/stats", () =>
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    // Toplam Kitap
    using var cmdBook = new NpgsqlCommand("SELECT COUNT(*) FROM Kitaplar", conn);
    var toplamKitap = Convert.ToInt32(cmdBook.ExecuteScalar());
    
    // Toplam Üye
    using var cmdMember = new NpgsqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE Rol = 'Uye'", conn);
    var toplamUye = Convert.ToInt32(cmdMember.ExecuteScalar());
    
    // Aktif Ödünç
    using var cmdLoan = new NpgsqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL", conn);
    var aktifOdunc = Convert.ToInt32(cmdLoan.ExecuteScalar());
    
    // Gecikenler
    using var cmdOverdue = new NpgsqlCommand("SELECT COUNT(*) FROM OduncIslemleri WHERE IadeTarihi IS NULL AND BeklenenIadeTarihi < NOW()", conn);
    var gecikenler = Convert.ToInt32(cmdOverdue.ExecuteScalar());
    
    return Results.Ok(new { 
        ToplamKitap = toplamKitap, 
        ToplamUye = toplamUye, 
        AktifOdunc = aktifOdunc, 
        Gecikenler = gecikenler 
    });
})
.WithName("DashboardStats")
.WithTags("Dashboard")
.AllowAnonymous();

app.MapGet("/api/dashboard/geciken-kitaplar", () =>
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var query = @"
        SELECT 
            o.IslemID,
            k.AdSoyad, 
            kt.Baslik AS KitapBaslik, 
            o.BeklenenIadeTarihi,
            EXTRACT(DAY FROM NOW() - o.BeklenenIadeTarihi)::INTEGER AS GecikmeGun
        FROM OduncIslemleri o
        JOIN Kullanicilar k ON o.UyeID = k.KullaniciID
        JOIN Kitaplar kt ON o.KitapID = kt.KitapID
        WHERE o.IadeTarihi IS NULL AND o.BeklenenIadeTarihi < NOW()
        ORDER BY o.BeklenenIadeTarihi ASC";
    
    using var cmd = new NpgsqlCommand(query, conn);
    using var reader = cmd.ExecuteReader();
    
    var list = new List<object>();
    while (reader.Read())
    {
        list.Add(new {
            IslemID = reader.GetInt32(0),
            AdSoyad = reader.GetString(1),
            KitapBaslik = reader.GetString(2),
            BeklenenIadeTarihi = reader.GetDateTime(3),
            GecikmeGun = reader.GetInt32(4)
        });
    }
    
    return Results.Ok(list);
})
.WithName("GecikenKitaplar")
.WithTags("Dashboard")
.AllowAnonymous();

// ==================== ŞİFRE SIFIRLAMA API ====================

app.MapPost("/api/auth/sifremi-unuttum", async (ForgotPasswordRequest request, IEmailService emailService) =>
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();

    var cmd = new NpgsqlCommand(@"SELECT KullaniciID, AdSoyad FROM Kullanicilar WHERE Email = @email AND AktifMi = TRUE", conn);
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

    var random = new Random();
    var kod = random.Next(100000, 999999).ToString();
    var sonKullanma = DateTime.Now.AddMinutes(15);

    var insertCmd = new NpgsqlCommand(@"
        INSERT INTO SifreSifirlamaIslemleri (KullaniciID, Kod, SonKullanmaTarihi, KullanildiMi)
        VALUES (@uid, @kod, @skk, FALSE)", conn);
    insertCmd.Parameters.AddWithValue("@uid", userId);
    insertCmd.Parameters.AddWithValue("@kod", kod);
    insertCmd.Parameters.AddWithValue("@skk", sonKullanma);
    await insertCmd.ExecuteNonQueryAsync();

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

    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();

    var cmd = new NpgsqlCommand(@"
        SELECT IslemID, KullaniciID FROM SifreSifirlamaIslemleri 
        WHERE Kod = @kod AND KullanildiMi = FALSE AND SonKullanmaTarihi > NOW()
        ORDER BY IslemID DESC LIMIT 1", conn);
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

    var hash = HashPassword(request.YeniSifre);
    var updateCmd = new NpgsqlCommand(@"UPDATE Kullanicilar SET Sifre = @pass WHERE KullaniciID = @uid", conn);
    updateCmd.Parameters.AddWithValue("@pass", hash);
    updateCmd.Parameters.AddWithValue("@uid", userId);
    await updateCmd.ExecuteNonQueryAsync();

    var expireCmd = new NpgsqlCommand(@"UPDATE SifreSifirlamaIslemleri SET KullanildiMi = TRUE WHERE IslemID = @iid", conn);
    expireCmd.Parameters.AddWithValue("@iid", islemId);
    await expireCmd.ExecuteNonQueryAsync();

    return Results.Ok(new { message = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz." });
})
.WithName("SifreSifirla")
.WithTags("Giriş")
.AllowAnonymous();

app.MapPost("/api/auth/register", async (RegisterRequest request, IEmailService emailService) =>
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();

    if (!request.Email.EndsWith("@gmail.com"))
        return Results.BadRequest(new { message = "Sadece @gmail.com uzantılı mail adresleri kabul edilmektedir." });

    if (request.Sifre.Length < 6)
        return Results.BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });

    var checkCmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @user OR Email = @email", conn);
    checkCmd.Parameters.AddWithValue("@user", request.KullaniciAdi);
    checkCmd.Parameters.AddWithValue("@email", request.Email);
    if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
        return Results.BadRequest(new { message = "Bu kullanıcı adı veya e-posta adresi zaten kullanılıyor." });

    var hash = HashPassword(request.Sifre);
    var insertCmd = new NpgsqlCommand(@"
        INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol, AktifMi) 
        VALUES (@user, @pass, @ad, @email, @tel, 'Uye', FALSE)
        RETURNING KullaniciID", conn);
    
    insertCmd.Parameters.AddWithValue("@user", request.KullaniciAdi);
    insertCmd.Parameters.AddWithValue("@pass", hash);
    insertCmd.Parameters.AddWithValue("@ad", request.AdSoyad);
    insertCmd.Parameters.AddWithValue("@email", request.Email);
    insertCmd.Parameters.AddWithValue("@tel", (object?)request.Telefon ?? DBNull.Value);
    
    var userId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

    var random = new Random();
    var kod = random.Next(100000, 999999).ToString();
    var sonKullanma = DateTime.Now.AddMinutes(15);

    var codeCmd = new NpgsqlCommand(@"
        INSERT INTO SifreSifirlamaIslemleri (KullaniciID, Kod, SonKullanmaTarihi, KullanildiMi)
        VALUES (@uid, @kod, @skk, FALSE)", conn);
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();

    var cmd = new NpgsqlCommand(@"
        SELECT IslemID, KullaniciID FROM SifreSifirlamaIslemleri 
        WHERE Kod = @kod AND KullaniciID = @uid AND KullanildiMi = FALSE AND SonKullanmaTarihi > NOW()
        ORDER BY IslemID DESC LIMIT 1", conn);
    cmd.Parameters.AddWithValue("@kod", request.Kod);
    cmd.Parameters.AddWithValue("@uid", request.UserId);

    int islemId = 0;
    
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.Read()) islemId = reader.GetInt32(0);
    }

    if (islemId == 0) return Results.BadRequest(new { message = "Geçersiz veya süresi dolmuş kod." });

    var activateCmd = new NpgsqlCommand(@"UPDATE Kullanicilar SET AktifMi = TRUE WHERE KullaniciID = @uid", conn);
    activateCmd.Parameters.AddWithValue("@uid", request.UserId);
    await activateCmd.ExecuteNonQueryAsync();
    
    var expireCmd = new NpgsqlCommand(@"UPDATE SifreSifirlamaIslemleri SET KullanildiMi = TRUE WHERE IslemID = @iid", conn);
    expireCmd.Parameters.AddWithValue("@iid", islemId);
    await expireCmd.ExecuteNonQueryAsync();

    return Results.Ok(new { message = "Hesabınız başarıyla doğrulandı. Giriş yapabilirsiniz." });
})
.WithName("VerifyEmail")
.WithTags("Giriş")
.AllowAnonymous();

// ==================== KİTAPLAR API ====================

app.MapGet("/api/kitaplar", () =>
{
    var kitaplar = new List<object>();
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"
        SELECT k.KitapID, k.Baslik, k.Yazar, COALESCE(k.ISBN, '') as ISBN, 
               COALESCE(kt.TurAdi, '-') as TurAdi, k.StokAdedi, k.MevcutAdet, COALESCE(k.RafNo, '') as RafNo
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
.RequireAuthorization();

app.MapGet("/api/kitaplar/{id}", (int id) =>
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"SELECT * FROM Kitaplar WHERE KitapID = @id", conn);
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"
        INSERT INTO Kitaplar (Baslik, Yazar, ISBN, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo)
        VALUES (@baslik, @yazar, @isbn, @yil, @tur, @stok, @stok, @raf)
        RETURNING KitapID", conn);
    
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"DELETE FROM Kitaplar WHERE KitapID = @id", conn);
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"
        SELECT KullaniciID, KullaniciAdi, AdSoyad, COALESCE(Email, '') as Email, 
               COALESCE(Telefon, '') as Telefon, Rol, AktifMi
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"SELECT * FROM Kullanicilar WHERE KullaniciID = @id", conn);
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();

    string? emailAdresi = null;
    string adSoyad = "";
    
    using (var userCmd = new NpgsqlCommand(@"SELECT Email, AdSoyad FROM Kullanicilar WHERE KullaniciID = @uye", conn))
    {
        userCmd.Parameters.AddWithValue("@uye", request.UyeID);
        using var reader = await userCmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            emailAdresi = reader.IsDBNull(0) ? null : reader.GetString(0);
            adSoyad = reader.GetString(1);
        }
    }

    var cmd = new NpgsqlCommand(@"
        INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi) 
        VALUES (@kitap, @uye, NOW() + INTERVAL '1 day' * @gun)
        RETURNING IslemID", conn);
    
    cmd.Parameters.AddWithValue("@kitap", request.KitapID);
    cmd.Parameters.AddWithValue("@uye", request.UyeID);
    cmd.Parameters.AddWithValue("@gun", request.OduncGunu ?? 14);
    
    var id = await cmd.ExecuteScalarAsync();

    var updateStock = new NpgsqlCommand(@"UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = @kitap", conn);
    updateStock.Parameters.AddWithValue("@kitap", request.KitapID);
    await updateStock.ExecuteNonQueryAsync();

    if (!string.IsNullOrEmpty(emailAdresi))
    {
        try 
        {
            string kitapAdi = "";
            using (var kCmd = new NpgsqlCommand(@"SELECT Baslik FROM Kitaplar WHERE KitapID = @id", conn))
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    // Get KitapID first
    var getKitapCmd = new NpgsqlCommand(@"SELECT KitapID FROM OduncIslemleri WHERE IslemID = @id", conn);
    getKitapCmd.Parameters.AddWithValue("@id", id);
    var kitapId = getKitapCmd.ExecuteScalar();
    
    if (kitapId == null) return Results.NotFound();
    
    var updateOdunc = new NpgsqlCommand(@"UPDATE OduncIslemleri SET Durum = 'IadeEdildi', IadeTarihi = NOW() WHERE IslemID = @id", conn);
    updateOdunc.Parameters.AddWithValue("@id", id);
    updateOdunc.ExecuteNonQuery();
    
    var updateKitap = new NpgsqlCommand(@"UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 WHERE KitapID = @kitapId", conn);
    updateKitap.Parameters.AddWithValue("@kitapId", kitapId);
    updateKitap.ExecuteNonQuery();
    
    return Results.Ok(new { message = "Kitap iade alındı" });
})
.WithName("IadeAl")
.WithTags("Ödünç İşlemleri")
.RequireAuthorization();

app.MapGet("/api/odunc/uye/{uyeId}", (int uyeId) =>
{
    var islemler = new List<object>();
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"
        SELECT o.IslemID, k.Baslik, k.Yazar, o.OduncTarihi, o.BeklenenIadeTarihi, o.IadeTarihi, o.Durum,
            CASE 
                WHEN o.Durum = 'Odunc' AND o.BeklenenIadeTarihi < NOW() 
                THEN EXTRACT(DAY FROM NOW() - o.BeklenenIadeTarihi)::INTEGER
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var cmd = new NpgsqlCommand(@"SELECT TurID, TurAdi FROM KitapTurleri", conn);
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
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    var kitapSayisi = new NpgsqlCommand(@"SELECT COUNT(*) FROM Kitaplar", conn).ExecuteScalar();
    var uyeSayisi = new NpgsqlCommand(@"SELECT COUNT(*) FROM Kullanicilar WHERE Rol = 'Uye'", conn).ExecuteScalar();
    var oduncte = new NpgsqlCommand(@"SELECT COUNT(*) FROM OduncIslemleri WHERE Durum = 'Odunc'", conn).ExecuteScalar();
    var geciken = new NpgsqlCommand(@"SELECT COUNT(*) FROM OduncIslemleri WHERE Durum = 'Odunc' AND BeklenenIadeTarihi < NOW()", conn).ExecuteScalar();
    
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

