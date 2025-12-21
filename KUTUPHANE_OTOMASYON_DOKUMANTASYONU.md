# 📚 KÜTÜPHANE OTOMASYON SİSTEMİ
## Teknik Dokümantasyon

---

**Proje Adı:** Kütüphane Otomasyon Sistemi  
**Geliştiriciler:** Muhammed Ali Aral, Yağız Van  
**Tarih:** Aralık 2024  
**Versiyon:** 1.0  

---

# İÇİNDEKİLER

1. [Proje Özeti](#1-proje-özeti)
2. [Sistem Gereksinimleri](#2-sistem-gereksinimleri)
3. [Sistem Mimarisi ve Bulut Altyapısı](#3-sistem-mimarisi-ve-bulut-altyapısı)
4. [Teknoloji Yığını](#4-teknoloji-yığını)
5. [Proje Yapısı](#5-proje-yapısı)
6. [Veritabanı Tasarımı](#6-veritabanı-tasarımı)
7. [REST API Dokümantasyonu](#7-rest-api-dokümantasyonu)
8. [WPF Masaüstü Uygulaması](#8-wpf-masaüstü-uygulaması)
9. [Güvenlik Özellikleri](#9-güvenlik-özellikleri)
10. [Kurulum ve Çalıştırma](#10-kurulum-ve-çalıştırma)
11. [Kaynak Kod Detayları](#11-kaynak-kod-detayları)
12. [Web Sitesi (Web Arayüzü)](#12-web-sitesi-web-arayüzü)
13. [Mobil Uygulama](#13-mobil-uygulama)

---

# 1. PROJE ÖZETİ

## 1.1 Genel Bakış

Kütüphane Otomasyon Sistemi, modern ve kullanıcı dostu bir kütüphane yönetim yazılımıdır. Sistem, WPF (Windows Presentation Foundation) teknolojisi ile geliştirilmiş masaüstü uygulaması ve ASP.NET Core ile geliştirilmiş REST API'den oluşmaktadır.

## 1.2 Temel Özellikler

### Kullanıcı Sistemi
- **Yönetici Paneli:** Tüm işlemlere tam erişim
- **Üye Paneli:** Kitap görüntüleme, değerlendirme ve kişisel ödünç takibi
- Güvenli giriş sistemi (SHA256 şifreleme + JWT)
- Gmail ile şifremi unuttum özelliği
- E-posta doğrulama ile kayıt

### Kitap İşlemleri
- Kitap ekleme, düzenleme ve silme
- Toplu kitap silme (akıllı ödünç kontrolü)
- Excel'den içe/dışa aktarma
- Barkod tarama ile hızlı işlem
- ISBN-10 ve ISBN-13 doğrulama
- Kitap türü yönetimi
- Stok takibi
- Kitap değerlendirme ve yorum sistemi

### Üye İşlemleri
- Yeni üye kaydı (Gmail doğrulama)
- Üye bilgilerini güncelleme
- Üyeleri aktif/pasif yapma
- Akıllı silme (ilişkili kayıtları temizler)

### Ödünç İşlemleri
- Kitap ödünç verme
- İade alma
- Geciken kitapları takip etme
- Gecikme ücreti hesaplama
- Filtreleme (Tümü, Ödünçte, Geciken, İade Edilmiş)

### Raporlar ve İstatistikler
- Dashboard istatistikleri (gerçek zamanlı)
- Geciken kitaplar listesi
- Excel rapor çıktısı

---

# 2. SİSTEM GEREKSİNİMLERİ

## 2.1 Geliştirme Ortamı
- .NET 8.0 SDK
- Visual Studio 2022 veya Visual Studio Code
- Git (versiyon kontrolü için)

## 2.2 Çalıştırma Ortamı
- Windows 10/11 (WPF uygulaması için)
- PostgreSQL veritabanı (Supabase üzerinden)
- İnternet bağlantısı (bulut veritabanı için)

## 2.3 Bağımlılıklar

### API Bağımlılıkları (NuGet Paketleri)
| Paket | Versiyon | Açıklama |
|-------|----------|----------|
| MailKit | 4.14.1 | E-posta gönderimi |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.0 | JWT kimlik doğrulama |
| Microsoft.AspNetCore.OpenApi | 8.0.0 | OpenAPI desteği |
| Swashbuckle.AspNetCore | 6.5.0 | Swagger UI |
| Npgsql | 8.0.5 | PostgreSQL bağlantısı |

### WPF Bağımlılıkları (NuGet Paketleri)
| Paket | Versiyon | Açıklama |
|-------|----------|----------|
| MaterialDesignThemes | 4.9.0 | Material Design UI |
| MaterialDesignColors | 2.1.4 | Material Design renk paleti |
| ClosedXML | 0.102.2 | Excel işlemleri |
| ZXing.Net | 0.16.11 | Barkod okuma |
| ZXing.Net.Bindings.Windows.Compatibility | 0.16.14 | Windows barkod desteği |
| AForge.Video.DirectShow | 2.2.5 | Kamera erişimi |
| Npgsql | 8.0.5 | PostgreSQL bağlantısı |

---

# 3. SİSTEM MİMARİSİ VE BULUT ALTYAPISI

## 3.1 Genel Mimari

Kütüphane Otomasyon Sistemi, modern ve ölçeklenebilir bir **3-Katmanlı Mimari** kullanmaktadır:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              İSTEMCİ KATMANI                                    │
├──────────────────────────┬──────────────────────────┬───────────────────────────┤
│  WPF Masaüstü Uygulaması │   Web Tarayıcı Arayüzü   │     Mobil Uygulama        │
│  (Windows 10/11)         │   (HTML/CSS/JavaScript)  │     (PWA + Android)       │
├──────────────────────────┴──────────────────────────┴───────────────────────────┤
│                                     ▼                                           │
│                              HTTP/HTTPS                                         │
│                                     ▼                                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│                              API KATMANI                                        │
│                       ASP.NET Core 8.0 Minimal API                              │
│                 (JWT Authentication, Swagger, CORS)                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                     ▼                                           │
│                             SQL/PostgreSQL                                      │
│                                     ▼                                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│                            VERİTABANI KATMANI                                   │
│                       Supabase PostgreSQL (Bulut)                               │
│                 (AWS EU-Central-1, Transaction Pooler)                          │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## 3.2 Supabase Bulut Veritabanı

Supabase, açık kaynaklı bir Firebase alternatifi olup, PostgreSQL veritabanını bulut ortamında sunar.

### 3.2.1 Neden Supabase?

| Özellik | Açıklama |
|---------|----------|
| **Bulut Tabanlı** | Veritabanı internette barındırılır, her yerden erişilebilir |
| **PostgreSQL** | Güçlü, açık kaynaklı ilişkisel veritabanı |
| **Otomatik Ölçekleme** | Sunucu yönetimi gerektirmez |
| **SSL Şifreleme** | Tüm bağlantılar güvenli |
| **Connection Pooling** | Verimli bağlantı yönetimi |
| **Ücretsiz Plan** | Küçük/orta projeler için yeterli |

### 3.2.2 Bağlantı Detayları

| Parametre | Değer |
|-----------|-------|
| **Host** | aws-1-eu-central-1.pooler.supabase.com |
| **Port** | 6543 (Transaction Pooler) |
| **Database** | postgres |
| **SSL Mode** | Require |
| **Region** | EU-Central-1 (Frankfurt) |

### 3.2.3 Bağlantı String'i (C#)

```csharp
var connectionString = 
    "Host=aws-1-eu-central-1.pooler.supabase.com;" +
    "Port=6543;" +
    "Database=postgres;" +
    "Username=postgres.xxxxx;" +
    "Password=*****;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true;" +
    "Multiplexing=false;" +
    "No Reset On Close=true;" +
    "Pooling=true;" +
    "Minimum Pool Size=2;" +
    "Maximum Pool Size=20";
```

### 3.2.4 Transaction Pooler Avantajları

- IPv4 uyumluluğu (IPv6 gerektirmez)
- Bağlantı paylaşımı ile kaynak tasarrufu
- Daha hızlı bağlantı süresi
- Yüksek eşzamanlı kullanıcı desteği

## 3.3 API Katmanı Mimarisi

API, tüm istemciler (WPF, Web) ile veritabanı arasında köprü görevi görür.

### 3.3.1 Veri Akışı

```
[WPF Uygulaması]         [Web Tarayıcı]         [Mobil Uygulama]
       │                       │                       │
       │ ApiService.cs         │ api.js                │ api.js
       │ (HTTP Client)         │ (Fetch API)           │ (Fetch API)
       ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────┐
│                        REST API                             │
│                 http://localhost:5026                       │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ JWT Auth     │  │ Swagger UI   │  │ CORS         │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                             │
│  Endpoint'ler:                                              │
│  • /api/giris        → Kimlik doğrulama                    │
│  • /api/kitaplar     → Kitap CRUD                          │
│  • /api/uyeler       → Üye CRUD                            │
│  • /api/odunc        → Ödünç işlemleri                     │
│  • /api/raporlar     → Raporlar                            │
│  • /api/dashboard    → İstatistikler                       │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Npgsql (PostgreSQL Driver)
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              Supabase PostgreSQL                            │
│        aws-1-eu-central-1.pooler.supabase.com               │
│                                                             │
│  Tablolar:                                                  │
│  • Kullanicilar    • Kitaplar                              │
│  • OduncIslemleri  • KitapTurleri                          │
│  • Degerlendirmeler • Ayarlar                              │
└─────────────────────────────────────────────────────────────┘
```

### 3.3.2 API Katmanının Görevleri

| Görev | Açıklama |
|-------|----------|
| **Veri Erişimi** | Veritabanı sorgularını çalıştırır |
| **İş Mantığı** | Gecikme hesaplama, stok güncelleme |
| **Kimlik Doğrulama** | JWT token oluşturma ve doğrulama |
| **Yetkilendirme** | Rol bazlı erişim kontrolü |
| **Veri Dönüştürme** | SQL sonuçlarını JSON'a çevirir |
| **Güvenlik** | SQL Injection koruması, şifre hashleme |

### 3.3.3 Neden API Katmanı Kullanılıyor?

✅ **Güvenlik:** Veritabanı bilgileri istemcilerde saklanmaz  
✅ **Merkezi Yönetim:** İş mantığı tek yerde yönetilir  
✅ **Esneklik:** Farklı platformlar (WPF, Web, Mobil) aynı API'yi kullanabilir  
✅ **Ölçeklenebilirlik:** API bağımsız olarak ölçeklenebilir  
✅ **Bakım Kolaylığı:** Değişiklikler sadece API'de yapılır  

## 3.4 İstemci-API İletişimi

### 3.4.1 WPF Uygulaması (ApiService.cs)

```csharp
// API Base URL
private static string _baseUrl = "http://localhost:5000";

// JWT Token yönetimi
public static string? Token { get; set; }

// Örnek API çağrısı
public static async Task<List<KitapDto>?> GetKitaplarAsync()
{
    return await _httpClient.GetFromJsonAsync<List<KitapDto>>(
        $"{_baseUrl}/api/kitaplar");
}
```

### 3.4.2 Web Arayüzü (api.js)

```javascript
const API_BASE = 'http://localhost:5000/api';

async function getKitaplar() {
    const response = await fetch(`${API_BASE}/kitaplar`, {
        headers: {
            'Authorization': `Bearer ${Auth.getToken()}`
        }
    });
    return await response.json();
}
```

## 3.5 Avantajlar ve Dezavantajlar

### Avantajlar

| Avantaj | Açıklama |
|---------|----------|
| 🌐 **Her Yerden Erişim** | İnternet olan her yerden çalışır |
| 🔒 **Güvenli** | SSL/TLS şifreleme, JWT auth |
| 📱 **Çoklu Platform** | WPF + Web aynı anda |
| ⚡ **Performans** | Connection pooling ile hızlı |
| 💾 **Yedekleme** | Supabase otomatik yedekleme |
| 🆓 **Maliyet** | Küçük projeler için ücretsiz |

### Dikkat Edilmesi Gerekenler

| Konu | Açıklama |
|------|----------|
| 🌍 **İnternet Bağımlılığı** | Çevrimdışı çalışmaz |
| ⏱️ **Gecikme** | Uzak sunucu nedeniyle hafif gecikme |
| 📊 **Kota Limitleri** | Ücretsiz planda kısıtlamalar |

---

# 4. TEKNOLOJİ YIĞINI

| Bileşen | Teknoloji |
|---------|-----------|
| Masaüstü Uygulama | .NET 8.0 WPF + Material Design |
| REST API | ASP.NET Core 8.0 Minimal API |
| Veritabanı | PostgreSQL (Supabase Bulut) |
| Authentication | JWT Bearer Token |
| Excel İşlemleri | ClosedXML |
| Barkod | ZXing.Net + AForge.Video |
| E-posta | MailKit (Gmail SMTP) |
| Veritabanı Driver | Npgsql (PostgreSQL) |

---

# 5. PROJE YAPISI

```
kutuphane-otomasyonu/
├── api/                          # REST API projesi
│   ├── Program.cs                # API endpoint'leri (1900+ satır)
│   ├── Services/                 # Servis katmanı
│   │   ├── EmailService.cs       # E-posta gönderim servisi
│   │   └── IEmailService.cs      # E-posta servis arayüzü
│   ├── Properties/               # Proje özellikleri
│   ├── appsettings.json          # JWT ve Email yapılandırması
│   ├── appsettings.Development.json
│   ├── KutuphaneApi.csproj       # API proje dosyası
│   └── KutuphaneApi.http         # HTTP test dosyası
│
├── csharp/                       # WPF masaüstü uygulaması
│   ├── Views/                    # Ana pencereler
│   │   ├── LoginWindow.xaml(.cs) # Giriş ekranı
│   │   ├── RegisterWindow.xaml(.cs) # Kayıt ekranı
│   │   ├── AdminWindow.xaml(.cs) # Yönetici paneli
│   │   ├── MemberWindow.xaml(.cs) # Üye paneli
│   │   └── ForgotPasswordWindow.xaml(.cs) # Şifre sıfırlama
│   │
│   ├── Pages/                    # Yönetici sayfaları
│   │   ├── DashboardPage.xaml(.cs) # Ana panel
│   │   ├── KitaplarPage.xaml(.cs) # Kitap yönetimi
│   │   ├── KitapDialog.xaml(.cs) # Kitap ekleme/düzenleme
│   │   ├── KitapDetayDialog.xaml(.cs) # Kitap detay ve yorumlar
│   │   ├── UyelerPage.xaml(.cs) # Üye yönetimi
│   │   ├── UyeDialog.xaml(.cs) # Üye ekleme
│   │   ├── OduncPage.xaml(.cs) # Ödünç işlemleri
│   │   ├── OduncDialog.xaml(.cs) # Ödünç verme
│   │   ├── RaporlarPage.xaml(.cs) # Raporlar
│   │   ├── AyarlarPage.xaml(.cs) # Sistem ayarları
│   │   └── BarcodeScannerDialog.xaml(.cs) # Barkod tarama
│   │
│   ├── MemberPages/              # Üye sayfaları
│   │   ├── AnasayfaPage.xaml(.cs) # Üye ana sayfa
│   │   ├── KitaplarViewPage.xaml(.cs) # Kitap listesi
│   │   ├── OdunclerimPage.xaml(.cs) # Ödünçlerim
│   │   └── ProfilPage.xaml(.cs) # Profil bilgileri
│   │
│   ├── Assets/                   # Görseller
│   │   └── logo.ico              # Uygulama ikonu
│   │
│   ├── App.xaml(.cs)             # Uygulama başlangıç noktası
│   ├── MainWindow.xaml(.cs)      # Ana pencere
│   ├── ApiService.cs             # API iletişim servisi (650 satır)
│   ├── DatabaseHelper.cs         # Supabase veritabanı yardımcı sınıfı
│   ├── CurrentSession.cs         # Oturum bilgileri
│   ├── DarkModeHelper.cs         # Karanlık mod desteği
│   └── KutuphaneOtomasyon.csproj # WPF proje dosyası
│
├── website/                      # Web arayüzü
│   ├── css/styles.css            # Stil dosyaları
│   ├── js/api.js                 # API iletişim modülü
│   ├── admin/                    # Yönetici paneli sayfaları
│   └── *.html                    # Üye sayfaları
│
├── mobile/                       # Mobil uygulama (PWA + Android)
│   ├── index.html                # Ana HTML (SPA)
│   ├── manifest.json             # PWA manifest
│   ├── sw.js                     # Service Worker
│   ├── capacitor.config.json     # Capacitor yapılandırması
│   ├── css/mobile.css            # Mobil stiller
│   ├── js/                       # JavaScript dosyaları
│   │   ├── api.js                # API iletişim
│   │   └── app.js                # Uygulama mantığı
│   └── android/                  # Native Android projesi
│
├── kutuphane-otomasyonu.sln      # Solution dosyası
├── render.yaml                   # Render.com dağıtım yapılandırması
├── .gitignore                    # Git ignore dosyası
└── README.md                     # Proje açıklaması
```

---

# 6. VERİTABANI TASARIMI

## 6.1 Veritabanı Bağlantısı

Sistem, Supabase üzerinde barındırılan PostgreSQL veritabanını kullanmaktadır.

**Bağlantı Bilgileri:**
- **Host:** aws-1-eu-central-1.pooler.supabase.com
- **Port:** 6543
- **Database:** postgres
- **SSL Mode:** Require

## 6.2 Tablo Yapıları

### 6.2.1 Kullanicilar Tablosu

```sql
CREATE TABLE Kullanicilar (
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
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| KullaniciID | SERIAL | Birincil anahtar, otomatik artan |
| KullaniciAdi | VARCHAR(50) | Benzersiz kullanıcı adı |
| Sifre | VARCHAR(256) | SHA256 ile hashlenmiş şifre |
| AdSoyad | VARCHAR(100) | Kullanıcının tam adı |
| Email | VARCHAR(100) | E-posta adresi |
| Telefon | VARCHAR(20) | Telefon numarası |
| Rol | VARCHAR(20) | 'Yonetici' veya 'Uye' |
| AktifMi | BOOLEAN | Hesap aktif mi? |
| OlusturmaTarihi | TIMESTAMP | Kayıt tarihi |

### 6.2.2 KitapTurleri Tablosu

```sql
CREATE TABLE KitapTurleri (
    TurID SERIAL PRIMARY KEY,
    TurAdi VARCHAR(50) NOT NULL
);
```

**Varsayılan Türler:** Roman, Hikaye, Şiir, Tarih, Bilim, Felsefe, Çocuk, Eğitim

### 6.2.3 Kitaplar Tablosu

```sql
CREATE TABLE Kitaplar (
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
```

### 6.2.4 OduncIslemleri Tablosu

```sql
CREATE TABLE OduncIslemleri (
    IslemID SERIAL PRIMARY KEY,
    KitapID INTEGER REFERENCES Kitaplar(KitapID),
    UyeID INTEGER REFERENCES Kullanicilar(KullaniciID),
    OduncTarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    BeklenenIadeTarihi TIMESTAMP,
    IadeTarihi TIMESTAMP,
    Durum VARCHAR(20) DEFAULT 'Odunc',
    CezaMiktari DECIMAL(10,2) DEFAULT 0
);
```

### 6.2.5 Degerlendirmeler Tablosu

```sql
CREATE TABLE Degerlendirmeler (
    DegerlendirmeID SERIAL PRIMARY KEY,
    UyeID INTEGER REFERENCES Kullanicilar(KullaniciID),
    KitapID INTEGER REFERENCES Kitaplar(KitapID),
    Puan SMALLINT CHECK (Puan >= 1 AND Puan <= 5),
    Yorum VARCHAR(500),
    Tarih TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### 6.2.6 SifreSifirlamaIslemleri Tablosu

```sql
CREATE TABLE SifreSifirlamaIslemleri (
    IslemID SERIAL PRIMARY KEY,
    KullaniciID INTEGER NOT NULL REFERENCES Kullanicilar(KullaniciID),
    Kod VARCHAR(10) NOT NULL,
    OlusturmaTarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    SonKullanmaTarihi TIMESTAMP NOT NULL,
    KullanildiMi BOOLEAN DEFAULT FALSE
);
```

### 6.2.7 Ayarlar Tablosu

```sql
CREATE TABLE Ayarlar (
    AyarID SERIAL PRIMARY KEY,
    AyarAdi VARCHAR(50) UNIQUE NOT NULL,
    AyarDegeri VARCHAR(100) NOT NULL,
    Aciklama VARCHAR(200)
);
```

**Varsayılan Ayarlar:**
| Ayar Adı | Değer | Açıklama |
|----------|-------|----------|
| GecikmeUcreti | 1.00 | Gün başına gecikme ücreti (TL) |
| MaxOduncGun | 14 | Maksimum ödünç verme süresi (gün) |

---

# 7. REST API DOKÜMANTASYONU

## 7.1 Genel Bilgiler

**Base URL:** http://localhost:5026  
**Swagger UI:** http://localhost:5026/swagger  
**Authentication:** JWT Bearer Token  

## 7.2 Kimlik Doğrulama (Authentication)

### 7.2.1 Giriş Yap

```
POST /api/giris
```

**Request Body:**
```json
{
    "KullaniciAdi": "string",
    "Sifre": "string"
}
```

**Response (Başarılı):**
```json
{
    "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "UserId": 1,
    "AdSoyad": "Sistem Yöneticisi",
    "Rol": "Yonetici",
    "Mesaj": "Giriş Başarılı"
}
```

### 7.2.2 Kayıt Ol

```
POST /api/auth/register
```

**Önemli:** Sadece @gmail.com uzantılı e-posta adresleri kabul edilmektedir.

### 7.2.3 E-posta Doğrulama

```
POST /api/auth/verify-email
```

### 7.2.4 Şifremi Unuttum

```
POST /api/auth/sifremi-unuttum
```

### 7.2.5 Şifre Sıfırla

```
POST /api/auth/sifre-sifirla
```

## 7.3 Dashboard Endpoint'leri

### 7.3.1 İstatistikler

```
GET /api/dashboard/stats
```

### 7.3.2 Geciken Kitaplar

```
GET /api/dashboard/geciken-kitaplar
```

## 7.4 Kitaplar Endpoint'leri

- `GET /api/kitaplar` - Kitap listesi
- `GET /api/kitaplar/{id}` - Kitap detayı
- `POST /api/kitaplar` - Kitap ekle
- `PUT /api/kitaplar/{id}` - Kitap güncelle
- `DELETE /api/kitaplar/{id}` - Kitap sil
- `DELETE /api/kitaplar/toplu` - Toplu kitap silme
- `POST /api/kitaplar/toplu` - Toplu kitap ekleme (Excel import)

## 7.5 Üyeler Endpoint'leri

- `GET /api/uyeler` - Üye listesi
- `GET /api/uyeler/{id}` - Üye detayı
- `POST /api/uyeler` - Üye ekle
- `DELETE /api/uyeler/{id}` - Üye sil

## 7.6 Ödünç İşlemleri Endpoint'leri

- `GET /api/odunc` - Ödünç listesi
- `POST /api/odunc` - Ödünç ver
- `PUT /api/odunc/{id}/iade` - İade al
- `GET /api/odunc/stats` - Ödünç istatistikleri

## 7.7 Kitap Türleri

```
GET /api/turler
```

## 7.8 Değerlendirmeler

- `GET /api/kitaplar/{kitapId}/degerlendirmeler` - Kitap değerlendirmeleri
- `GET /api/kitaplar/{kitapId}/puan` - Kitap ortalama puanı
- `DELETE /api/degerlendirmeler/{id}` - Değerlendirme sil

## 7.9 Raporlar

```
GET /api/raporlar
```

## 7.10 Üye Paneli Endpoint'leri

- `GET /api/uye/{uyeId}/stats` - Üye istatistikleri
- `GET /api/uye/{uyeId}/son-islemler` - Son işlemler
- `GET /api/uye/{uyeId}/oduncler` - Üye ödünçleri
- `GET /api/uye/{uyeId}/profil` - Profil bilgileri
- `PUT /api/uye/{uyeId}/profil` - Profil güncelleme

---

# 8. WPF MASAÜSTÜ UYGULAMASI

## 8.1 Uygulama Mimarisi

Uygulama, MVVM benzeri bir yapı kullanmaktadır:
- **Views:** Ana pencereler (Login, Register, Admin, Member)
- **Pages:** Sayfa içerikleri (Dashboard, Kitaplar, Üyeler, vb.)
- **Services:** API iletişim katmanı (ApiService)
- **Helpers:** Yardımcı sınıflar (DatabaseHelper, CurrentSession, DarkModeHelper)

## 8.2 Ana Pencereler

- **LoginWindow:** Kullanıcı girişi
- **RegisterWindow:** Yeni kullanıcı kaydı
- **AdminWindow:** Yönetici paneli
- **MemberWindow:** Üye paneli
- **ForgotPasswordWindow:** Şifre sıfırlama

## 8.3 Yönetici Sayfaları

- **DashboardPage:** İstatistik kartları ve geciken kitaplar
- **KitaplarPage:** Kitap yönetimi (CRUD, Excel import/export)
- **UyelerPage:** Üye yönetimi
- **OduncPage:** Ödünç işlemleri
- **RaporlarPage:** Raporlar ve istatistikler
- **AyarlarPage:** Sistem ayarları

## 8.4 Üye Sayfaları

- **AnasayfaPage:** Kişisel istatistikler
- **KitaplarViewPage:** Kitap listesi görüntüleme
- **OdunclerimPage:** Kullanıcının ödünçleri
- **ProfilPage:** Profil bilgileri ve güncelleme

---

# 9. GÜVENLİK ÖZELLİKLERİ

## 9.1 Şifre Güvenliği

- SHA256 algoritması ile hashleme
- Veritabanında düz metin şifre saklanmaz
- Minimum 6 karakter zorunluluğu

## 9.2 JWT Authentication

- 2 saat geçerlilik süresi
- HMAC-SHA256 imzalama
- Issuer ve Audience doğrulama

## 9.3 E-posta Doğrulama

- Sadece @gmail.com kabul edilir
- 6 haneli rastgele doğrulama kodu
- 15 dakika geçerlilik süresi

## 9.4 SQL Injection Koruması

Tüm veritabanı sorguları parametreli olarak yazılmıştır.

## 9.5 Yetkilendirme Kuralları

| İşlem | Yönetici | Üye |
|-------|----------|-----|
| Kitap Ekleme | ✅ | ❌ |
| Kitap Düzenleme | ✅ | ❌ |
| Kitap Silme | ✅ | ❌ |
| Üye Ekleme | ✅ | ❌ |
| Üye Silme | ✅ | ❌ |
| Ödünç Verme | ✅ | ❌ |
| İade Alma | ✅ | ❌ |
| Kitap Görüntüleme | ✅ | ✅ |
| Yorum Yapma | ✅ | ✅ |
| Kendi Yorumunu Silme | ✅ | ✅ |
| Başkasının Yorumunu Silme | ✅ | ❌ |
| Profil Güncelleme | ✅ | ✅ (Kendi) |

---

# 10. KURULUM VE ÇALIŞTIRMA

## 10.1 Gereksinimler

- .NET 8.0 SDK
- İnternet bağlantısı (Supabase veritabanı için)
- Windows 10/11 (WPF uygulaması için)

## 10.2 API'yi Başlatma

```bash
cd api
dotnet run
```

**Erişim Adresleri:**
- API: http://localhost:5026
- Swagger: http://localhost:5026/swagger

## 10.3 WPF Uygulamasını Başlatma

```bash
cd csharp
dotnet run
```

## 10.4 Varsayılan Giriş Bilgileri

| Alan | Değer |
|------|-------|
| Kullanıcı Adı | admin |
| Şifre | admin123 |

## 10.5 EXE Oluşturma (Tek Dosya)

```bash
cd csharp
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

# 11. KAYNAK KOD DETAYLARI

## 11.1 API Program.cs Özeti (1906 satır)

Program.cs dosyası, ASP.NET Core Minimal API yapısını kullanmaktadır.

**Bölümler:**
1. Servis Yapılandırması: Swagger, CORS, JWT Authentication
2. Veritabanı İlklendirme: Tablo oluşturma, varsayılan veriler
3. Giriş API: Login işlemi
4. Dashboard API: İstatistikler ve geciken kitaplar
5. Şifre Sıfırlama API: Şifre işlemleri
6. Kitaplar API: CRUD işlemleri
7. Üyeler API: CRUD işlemleri
8. Ödünç İşlemleri API: Ödünç ve iade
9. Değerlendirmeler API: Puan ve yorumlar
10. Raporlar API: Detaylı raporlar

## 11.2 Email Service

MailKit kullanarak Gmail SMTP üzerinden e-posta gönderir.

## 11.3 JWT Yapılandırması

```json
{
    "Jwt": {
        "Key": "KutuphaneOtomasyon_CokGizliVeGuvenliAnahtari_2025!",
        "Issuer": "http://localhost:5026",
        "Audience": "http://localhost:5026"
    }
}
```

---

# 12. WEB SİTESİ (WEB ARAYÜZÜ)

## 12.1 Genel Bakış

Proje, masaüstü uygulamasının yanı sıra modern ve responsive bir web arayüzü de içermektedir.

**Erişim:** http://localhost:5026 (API çalıştırıldığında)

## 12.2 Web Sitesi Yapısı

```
website/
├── css/styles.css              # Ana stil dosyası
├── js/api.js                   # API iletişim ve yardımcı fonksiyonlar
├── admin/                      # Yönetici paneli
│   ├── index.html              # Admin ana sayfa
│   ├── kitaplar.html           # Kitap yönetimi
│   ├── uyeler.html             # Üye yönetimi
│   └── odunc.html              # Ödünç işlemleri
├── index.html                  # Üye ana sayfa
├── login.html                  # Giriş sayfası
├── kitaplar.html               # Kitap listesi
├── odunclerim.html             # Kullanıcının ödünçleri
└── profil.html                 # Profil sayfası
```

## 12.3 Tasarım Özellikleri

- Modern karanlık tema
- Material Design ilkeleri
- Responsive tasarım
- Gradient butonlar
- Animasyonlu kartlar

## 12.4 API Entegrasyonu

Web sitesi, masaüstü uygulamasıyla aynı REST API'yi kullanır. Token yönetimi localStorage üzerinden yapılır.

---

# 13. DOCKER ENTEGRASYONU

## 13.1 Genel Bakış

Proje, Docker kullanılarak containerize edilmiştir. Bu sayede uygulama herhangi bir ortamda (geliştirme, test, production) aynı şekilde çalışabilir.

## 13.2 Docker Ne Yapıyor?

| Bileşen | Açıklama |
|---------|----------|
| **Container** | REST API'yi izole ortamda çalıştırır |
| **Port** | 5026 portunda yayın yapar |
| **Teknoloji** | ASP.NET Core 8.0 |
| **Veritabanı** | Supabase PostgreSQL (bulut) |

## 13.3 Docker Dosyaları

### api/Dockerfile
```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY api/KutuphaneApi.csproj ./api/
WORKDIR /src/api
RUN dotnet restore
COPY api/ ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5026
EXPOSE 5026
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'
services:
  api:
    build:
      context: .
      dockerfile: api/Dockerfile
    container_name: kutuphane-api
    ports:
      - "5026:5026"
    volumes:
      - ./website:/app/website:ro
      - ./mobile:/app/mobile:ro
    restart: unless-stopped
```

## 13.4 Kullanım Komutları

| Komut | Açıklama |
|-------|----------|
| `docker-compose up --build` | İlk kez başlat (build + run) |
| `docker-compose up -d` | Arka planda başlat |
| `docker-compose down` | Durdur |
| `docker logs kutuphane-api` | Logları gör |
| `docker ps` | Çalışan container'ları listele |

## 13.5 Docker vs Normal Çalıştırma

| Özellik | Normal (`dotnet run`) | Docker |
|---------|----------------------|--------|
| Gereksinim | .NET 8.0 SDK | Docker Desktop |
| Başlatma | `cd api && dotnet run` | `docker-compose up` |
| Port | 5026 | 5026 |
| Ortam | Sistem bağımlı | İzole container |
| Kullanım | Günlük geliştirme | Deploy, takım çalışması |

## 13.6 Erişim Adresleri (Docker Çalışırken)

| Adres | Açıklama |
|-------|----------|
| http://localhost:5026 | API ve Website |
| http://localhost:5026/swagger | Swagger UI |
| http://localhost:5026/mobile | Mobil Uygulama |

---

# 14. MOBİL UYGULAMA

## 13.1 Genel Bakış

Proje, PWA (Progressive Web App) tabanlı modern ve responsive bir mobil uygulama içermektedir. Uygulama ayrıca **Capacitor 8.0** kullanılarak native Android uygulamasına dönüştürülebilir. Mobil uygulama, Single Page Application (SPA) mimarisi ile geliştirilmiş olup, modern web teknolojileri kullanılarak tasarlanmıştır.

### 13.1.1 Uygulama Bilgileri

| Özellik | Değer |
|---------|-------|
| **Uygulama Adı** | Kütüphane Mobil |
| **Paket Adı** | com.kutuphane.mobil |
| **Versiyon** | 1.0.0 |
| **Minimum Android** | SDK 22 (Android 5.1) |
| **Hedef Android** | SDK 35 (Android 15) |
| **Mimari** | Single Page Application (SPA) |
| **Tema** | Dark Mode |

## 13.2 Mobil Uygulama Özellikleri

### 🚀 Splash Screen (Açılış Ekranı)
- Animasyonlu kitap ikonu (bounce efekti)
- Gradient arka plan (#667eea → #764ba2)
- Dönen yükleme animasyonu
- 1.5 saniye gösterim süresi
- Yumuşak geçiş efekti (fade out)

### 🔐 Giriş Sistemi
- Kullanıcı adı ve şifre ile giriş
- JWT Token tabanlı oturum yönetimi
- LocalStorage'da token saklama
- Otomatik oturum kontrolü
- Hatalı giriş bildirimi (shake animasyonu)
- Loading durumu gösterimi

### 📝 Kayıt Sistemi
- Yeni kullanıcı kaydı
- Zorunlu alanlar: Kullanıcı adı, Ad Soyad, E-posta, Şifre
- İsteğe bağlı: Telefon numarası
- **Sadece @gmail.com** e-posta adresleri kabul edilir
- Şifre en az 6 karakter olmalı
- Şifre tekrarı kontrolü
- E-posta doğrulama sistemi

### ✉️ E-posta Doğrulama
- Kayıt sonrası 6 haneli doğrulama kodu gönderimi
- Gmail adresine doğrulama kodu
- 15 dakika geçerlilik süresi
- Sayısal girdi optimizasyonu (inputmode="numeric")
- Başarılı doğrulama sonrası otomatik giriş sayfasına yönlendirme

### 📚 Kitaplar Sayfası
- **Tüm kitapları listeleme** (kart görünümü)
- **Gerçek zamanlı arama** (debounce: 300ms)
  - Kitap adına göre arama
  - Yazar adına göre arama
  - ISBN numarasına göre arama
- **Türe göre filtreleme** (chip butonları)
  - Dinamik tür listesi (API'den çekilir)
  - Aktif filtre vurgusu
- **Stok durumu gösterimi**
  - ✓ Mevcut (yeşil badge)
  - ✗ Stokta yok (kırmızı badge)
- **Kitap detay modal'ı**

### ⭐ Kitap Detay ve Değerlendirme
- Ortalama puan gösterimi (büyük font + yıldızlar)
- Değerlendirme sayısı
- **Değerlendirme formu:**
  - 1-5 yıldız seçimi (interaktif hover efekti)
  - İsteğe bağlı yorum yazma alanı
  - Değerlendirme gönderme
- **Yorumlar listesi:**
  - Kullanıcı adı, puan, tarih
  - Yorum metni
  - **Yorum silme** (sadece kendi yorumları veya admin)

### 📋 Ödünçlerim Sayfası
- **Sekme sistemi:**
  - **Aktif:** Devam eden ödünç işlemleri
  - **Geçmiş:** Tamamlanmış (iade edilmiş) işlemler
- **Ödünç kartları:**
  - Kitap adı
  - Alış tarihi
  - Son iade tarihi
  - İade tarihi (geçmiş için)
- **Durum göstergesi:**
  - 🟣 Aktif (kalan gün sayısı)
  - 🔴 Gecikmiş (gecikme gün sayısı)
  - 🟢 İade edildi
- **Admin için:** Tüm üyelerin ödünç işlemleri görünür

### 👤 Profil Sayfası
- **Avatar** (ad soyadın baş harfleri)
- **Kullanıcı bilgileri:**
  - Ad Soyad
  - E-posta
  - Telefon
  - Kayıt tarihi
  - Rol (Üye/Yönetici)
- **Profil düzenleme:**
  - Telefon numarası güncelleme
  - E-posta güncelleme
  - Modal ile düzenleme
- **Güvenli çıkış** butonu

### 🏠 Ana Sayfa
- **Hoşgeldiniz kartı** (gradient arka plan)
- **İstatistik kartları:**
  - Toplam kitap sayısı
  - Aktif ödünç sayısı
- **Hızlı işlemler:**
  - 🔍 Kitap Ara
  - 📋 Ödünçlerim
  - 👤 Profilim
- **Son eklenen kitaplar** (yatay kaydırma)

## 13.3 Teknoloji Yığını

| Teknoloji | Versiyon | Kullanım Amacı |
|-----------|----------|----------------|
| HTML5 | - | Uygulama yapısı (SPA) |
| CSS3 | - | Modern tasarım, Animasyonlar |
| JavaScript ES6+ | - | Uygulama mantığı, DOM manipülasyonu |
| PWA | - | Progressive Web App özellikleri |
| Service Worker | v1 | Offline destek, önbellek yönetimi |
| Capacitor | 8.0.0 | Native Android dönüşümü |
| @capacitor/core | 8.0.0 | Capacitor çekirdek kütüphanesi |
| @capacitor/cli | 8.0.0 | Capacitor komut satırı aracı |
| @capacitor/android | 8.0.0 | Android platform desteği |
| Inter Font | Google Fonts | Modern tipografi |

## 13.4 Dosya Yapısı (Detaylı)

```
mobile/
├── index.html              # Ana HTML - SPA (389 satır, 17.9 KB)
│   ├── Splash Screen       # Açılış ekranı
│   ├── Login Page          # Giriş sayfası
│   ├── Register Page       # Kayıt sayfası
│   ├── Verify Page         # E-posta doğrulama
│   ├── Main App Container  # Ana uygulama
│   │   ├── Header          # Üst başlık + avatar
│   │   ├── Home Section    # Ana sayfa
│   │   ├── Books Section   # Kitaplar
│   │   ├── Loans Section   # Ödünçlerim
│   │   └── Profile Section # Profil
│   ├── Bottom Navigation   # Alt navigasyon
│   ├── Book Modal          # Kitap detay modal
│   ├── Profile Edit Modal  # Profil düzenleme modal
│   └── Toast Notification  # Bildirim bileşeni
│
├── manifest.json           # PWA manifest (63 satır, 1.8 KB)
│   ├── Uygulama bilgileri
│   ├── İkon tanımları (8 boyut)
│   └── Tema ve renk ayarları
│
├── sw.js                   # Service Worker (74 satır, 2.1 KB)
│   ├── Cache yönetimi
│   ├── Install event
│   ├── Fetch event
│   └── Activate event
│
├── capacitor.config.json   # Capacitor yapılandırması (9 satır)
│
├── package.json            # Node.js bağımlılıkları (19 satır)
│
├── css/
│   └── mobile.css          # Mobil stil dosyası (1223 satır, 24.3 KB)
│       ├── CSS Variables   # Renk, spacing değişkenleri
│       ├── Splash Screen   # Açılış ekranı stilleri
│       ├── Login/Register  # Form stilleri
│       ├── Buttons         # Buton stilleri
│       ├── Main Layout     # Ana düzen
│       ├── Cards           # Kart bileşenleri
│       ├── Search          # Arama kutusu
│       ├── Books List      # Kitap listesi
│       ├── Loans List      # Ödünç listesi
│       ├── Profile         # Profil stilleri
│       ├── Bottom Nav      # Alt navigasyon
│       ├── Modal           # Modal stilleri
│       ├── Rating          # Yıldız değerlendirme
│       ├── Toast           # Bildirim stilleri
│       └── Utilities       # Yardımcı sınıflar
│
├── js/
│   ├── api.js              # API iletişim modülü (300 satır, 9 KB)
│   │   ├── Auth objesi     # Token yönetimi
│   │   ├── api objesi      # API istekleri
│   │   ├── Utils objesi    # Yardımcı fonksiyonlar
│   │   └── showToast()     # Bildirim fonksiyonu
│   │
│   └── app.js              # Uygulama mantığı (857 satır, 30.2 KB)
│       ├── initApp()       # Uygulama başlatma
│       ├── Giriş işlemleri
│       ├── Kayıt işlemleri
│       ├── Navigasyon
│       ├── Kitap işlemleri
│       ├── Ödünç işlemleri
│       ├── Profil işlemleri
│       └── PWA kayıt
│
├── icons/
│   └── icon.svg            # SVG uygulama ikonu
│
├── www/                    # Build çıktı klasörü (Capacitor için)
│   ├── index.html
│   ├── css/
│   ├── js/
│   └── ...
│
└── android/                # Native Android projesi
    ├── app/
    │   ├── src/main/
    │   │   ├── AndroidManifest.xml
    │   │   ├── java/com/kutuphane/mobil/
    │   │   │   └── MainActivity.java
    │   │   ├── assets/public/    # Web içeriği
    │   │   └── res/
    │   │       ├── drawable/     # Görseller
    │   │       ├── layout/       # Activity layout
    │   │       ├── mipmap-*/     # Uygulama ikonları
    │   │       ├── values/       # Renkler, stringler
    │   │       └── xml/          # Ayarlar
    │   └── build.gradle          # Uygulama build config
    ├── gradle/
    ├── build.gradle              # Root build config
    ├── settings.gradle
    ├── variables.gradle          # SDK versiyonları
    └── gradlew(.bat)             # Gradle wrapper
```

## 13.5 CSS Tasarım Sistemi

### 13.5.1 Renk Değişkenleri (CSS Variables)

```css
:root {
    /* Gradient'lar */
    --primary-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    --secondary-gradient: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    --success-gradient: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
    --warning-gradient: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    --danger-gradient: linear-gradient(135deg, #ff416c 0%, #ff4b2b 100%);

    /* Arka Plan Renkleri */
    --bg-primary: #0f0f1a;      /* Ana arka plan */
    --bg-secondary: #1a1a2e;    /* İkincil arka plan */
    --bg-card: #252545;         /* Kart arka planı */
    --bg-input: #1e1e35;        /* Input arka planı */

    /* Metin Renkleri */
    --text-primary: #ffffff;                    /* Ana metin */
    --text-secondary: rgba(255, 255, 255, 0.7); /* İkincil metin */
    --text-muted: rgba(255, 255, 255, 0.5);     /* Soluk metin */

    /* Kenarlık ve Gölge */
    --border-color: rgba(255, 255, 255, 0.1);
    --shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
    --shadow-sm: 0 4px 12px rgba(0, 0, 0, 0.3);

    /* Vurgu Renkleri */
    --accent-purple: #667eea;   /* Ana mor */
    --accent-pink: #f093fb;     /* Pembe */
    --accent-yellow: #ffd93d;   /* Sarı (yıldızlar) */
    --accent-green: #38ef7d;    /* Yeşil (başarı) */
    --accent-red: #ff416c;      /* Kırmızı (hata) */

    /* Spacing */
    --safe-area-top: env(safe-area-inset-top, 0px);
    --safe-area-bottom: env(safe-area-inset-bottom, 0px);
    --header-height: 60px;
    --nav-height: 70px;
}
```

### 13.5.2 Animasyonlar

| Animasyon | Açıklama | Süre |
|-----------|----------|------|
| `spin` | Dönen loader | 1s linear infinite |
| `bounce` | Zıplayan ikon (splash) | 1.5s infinite |
| `shake` | Sallanan hata | 0.5s ease |
| `fadeIn` | Görünür olma | 0.3s ease |

```css
@keyframes spin {
    to { transform: rotate(360deg); }
}

@keyframes bounce {
    0%, 100% { transform: translateY(0); }
    50% { transform: translateY(-10px); }
}

@keyframes shake {
    0%, 100% { transform: translateX(0); }
    25% { transform: translateX(-5px); }
    75% { transform: translateX(5px); }
}

@keyframes fadeIn {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: translateY(0); }
}
```

### 13.5.3 Responsive Tasarım

- **Safe Area Desteği:** Çentikli telefonlar için `env(safe-area-inset-*)` kullanımı
- **Landscape Modu:** Yatay mod için özel padding ayarları
- **Touch Optimizasyonu:** `-webkit-tap-highlight-color: transparent`
- **Smooth Scrolling:** `-webkit-overflow-scrolling: touch`
- **Maximum Scale:** `maximum-scale=1.0, user-scalable=no` (zoom engelleme)

## 13.6 JavaScript API Modülü (api.js)

### 13.6.1 Auth Objesi (Token Yönetimi)

```javascript
const Auth = {
    getToken: () => localStorage.getItem('kutuphane_mobile_token'),
    setToken: (token) => localStorage.setItem('kutuphane_mobile_token', token),
    removeToken: () => localStorage.removeItem('kutuphane_mobile_token'),

    getUser: () => {
        const user = localStorage.getItem('kutuphane_mobile_user');
        return user ? JSON.parse(user) : null;
    },
    setUser: (user) => localStorage.setItem('kutuphane_mobile_user', JSON.stringify(user)),
    removeUser: () => localStorage.removeItem('kutuphane_mobile_user'),

    isLoggedIn: () => {
        const token = Auth.getToken();
        const user = Auth.getUser();
        return token && user && user.id;
    },

    logout: () => {
        Auth.removeToken();
        Auth.removeUser();
    }
};
```

### 13.6.2 API Metotları

| Metod | Parametre | Dönüş | Açıklama |
|-------|-----------|-------|----------|
| `api.login(kullaniciAdi, sifre)` | string, string | {token, user} | Giriş yap |
| `api.register(kullaniciAdi, adSoyad, email, telefon, sifre)` | string x5 | {userId, message} | Kayıt ol |
| `api.verifyEmail(userId, kod)` | int, string | {message} | E-posta doğrula |
| `api.getKitaplar()` | - | Kitap[] | Tüm kitapları getir |
| `api.getKitap(id)` | int | Kitap | Tek kitap detayı |
| `api.getTurler()` | - | Tur[] | Kitap türlerini getir |
| `api.getOdunclerim()` | - | Odunc[] | Kullanıcının ödünçleri |
| `api.getIstatistikler()` | - | Stats | Dashboard istatistikleri |
| `api.getProfilBilgileri()` | - | Profil | Kullanıcı profili |
| `api.profilGuncelle(telefon, email)` | string, string | {message} | Profil güncelle |
| `api.getKitapDegerlendirmeleri(kitapId)` | int | Degerlendirme[] | Kitap yorumları |
| `api.getKitapPuan(kitapId)` | int | {ortalamaPuan, degerlendirmeSayisi} | Kitap puanı |
| `api.degerlendirmeEkle(kitapId, puan, yorum)` | int, int, string | {message} | Yorum ekle |
| `api.degerlendirmeSil(id)` | int | {message} | Yorum sil |

### 13.6.3 Utils Yardımcı Fonksiyonlar

```javascript
const Utils = {
    // Tarih formatla (DD.MM.YYYY)
    formatDate: (dateStr) => {
        if (!dateStr) return '-';
        const date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR', {
            day: '2-digit', month: '2-digit', year: 'numeric'
        });
    },

    // Ad soyadın baş harflerini al
    getInitials: (name) => {
        if (!name) return '?';
        return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
    },

    // Gecikme kontrolü
    isOverdue: (dueDate) => {
        if (!dueDate) return false;
        return new Date(dueDate) < new Date();
    },

    // Kalan gün hesapla
    daysRemaining: (dueDate) => {
        if (!dueDate) return 0;
        const diff = new Date(dueDate) - new Date();
        return Math.ceil(diff / (1000 * 60 * 60 * 24));
    }
};
```

## 13.7 Uygulama Mantığı (app.js)

### 13.7.1 Ana Fonksiyonlar

| Fonksiyon | Açıklama |
|-----------|----------|
| `initApp()` | Uygulamayı başlat, oturum kontrolü yap |
| `setupEventListeners()` | Tüm event listener'ları ayarla |
| `showLogin()` | Giriş sayfasını göster |
| `showRegister()` | Kayıt sayfasını göster |
| `showVerify(userId, email)` | Doğrulama sayfasını göster |
| `showMainApp()` | Ana uygulamayı göster |
| `handleLogin(e)` | Giriş formunu işle |
| `handleRegister(e)` | Kayıt formunu işle |
| `handleVerify(e)` | Doğrulama formunu işle |
| `logout()` | Çıkış yap |
| `loadData()` | Tüm verileri yükle (paralel) |
| `navigateTo(page)` | Sayfa değiştir |

### 13.7.2 Kitap Fonksiyonları

| Fonksiyon | Açıklama |
|-----------|----------|
| `setupFilterChips()` | Tür filtre butonlarını oluştur |
| `filterBooks()` | Kitapları filtrele (arama + tür) |
| `renderBooks(books)` | Kitap listesini render et |
| `renderRecentBooks()` | Son eklenen kitapları göster |
| `openBookModal(bookId)` | Kitap detay modal'ını aç |
| `closeModal()` | Modal'ı kapat |
| `setupStarRating()` | Yıldız seçimini ayarla |
| `updateStars(stars, rating)` | Yıldızları güncelle |
| `submitReview(bookId)` | Değerlendirme gönder |
| `deleteReview(reviewId, bookId)` | Yorumu sil |

### 13.7.3 Ödünç Fonksiyonları

| Fonksiyon | Açıklama |
|-----------|----------|
| `loadLoans()` | Ödünç işlemlerini yükle |
| `renderLoans(tab)` | Ödünç listesini render et (aktif/geçmiş) |

### 13.7.4 Profil Fonksiyonları

| Fonksiyon | Açıklama |
|-----------|----------|
| `loadProfile()` | Profil bilgilerini yükle |
| `openEditProfileModal()` | Profil düzenleme modal'ını aç |
| `closeProfileModal()` | Profil modal'ını kapat |
| `handleProfileUpdate(e)` | Profil güncelleme formunu işle |

### 13.7.5 Debounce Fonksiyonu

Arama kutusunda performans optimizasyonu için kullanılır:

```javascript
function debounce(func, wait) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), wait);
    };
}
```

## 13.8 PWA Özellikleri

### 13.8.1 Service Worker (sw.js)

```javascript
const CACHE_NAME = 'kutuphane-mobile-v1';
const urlsToCache = [
    './',
    './index.html',
    './css/mobile.css',
    './js/api.js',
    './js/app.js',
    './manifest.json'
];
```

**Olaylar:**
- **Install:** Önbelleğe alma
- **Fetch:** Önce cache, sonra network
- **Activate:** Eski cache'leri temizleme

### 13.8.2 Manifest Ayarları (manifest.json)

```json
{
    "name": "Kütüphane Mobil",
    "short_name": "Kütüphane",
    "description": "Kütüphane Otomasyon Sistemi Mobil Uygulaması",
    "start_url": "./index.html",
    "display": "standalone",
    "background_color": "#0f0f1a",
    "theme_color": "#667eea",
    "orientation": "portrait-primary",
    "categories": ["books", "education", "utilities"],
    "lang": "tr-TR"
}
```

**İkon Boyutları:**
- 72x72, 96x96, 128x128, 144x144
- 152x152, 192x192, 384x384, 512x512

### 13.8.3 Ana Ekrana Ekleme

Kullanıcılar uygulamayı telefonlarının ana ekranına ekleyerek native uygulama gibi kullanabilir:

1. **Chrome/Edge:** Menü → "Ana ekrana ekle"
2. **Safari:** Paylaş → "Ana Ekrana Ekle"

## 13.9 Capacitor Android Entegrasyonu

### 13.9.1 Yapılandırma (capacitor.config.json)

```json
{
    "appId": "com.kutuphane.mobil",
    "appName": "Kutuphane Mobil",
    "webDir": "www",
    "server": {
        "androidScheme": "http",
        "cleartext": true
    }
}
```

### 13.9.2 Android Gradle Ayarları

```gradle
// variables.gradle
ext {
    minSdkVersion = 22
    compileSdkVersion = 35
    targetSdkVersion = 35
    androidxAppCompatVersion = '1.7.0'
    // ...
}
```

### 13.9.3 Kurulum Adımları

```bash
# 1. Bağımlılıkları yükle
cd mobile
npm install

# 2. www klasörünü güncelle
cp index.html www/
cp -r css www/
cp -r js www/
cp manifest.json www/
cp sw.js www/

# 3. Android projesini senkronize et
npx cap sync android

# 4. Android Studio'da aç
npx cap open android
```

### 13.9.4 APK Oluşturma

1. Android Studio'da projeyi açın
2. **Build → Build Bundle(s) / APK(s) → Build APK(s)**
3. APK dosyası: `android/app/build/outputs/apk/debug/app-debug.apk`

**Release APK için:**
1. **Build → Generate Signed Bundle / APK**
2. Keystore oluşturun veya mevcut olanı kullanın
3. Release build seçin

## 13.10 API Endpoint'leri (Tam Liste)

| Endpoint | Metod | Body | Açıklama |
|----------|-------|------|----------|
| `/api/giris` | POST | `{KullaniciAdi, Sifre}` | Kullanıcı girişi |
| `/api/auth/register` | POST | `{KullaniciAdi, AdSoyad, Email, Telefon, Sifre}` | Kayıt ol |
| `/api/auth/verify-email` | POST | `{UserId, Kod}` | E-posta doğrula |
| `/api/kitaplar` | GET | - | Kitap listesi |
| `/api/kitaplar/{id}` | GET | - | Kitap detayı |
| `/api/turler` | GET | - | Kitap türleri |
| `/api/odunc` | GET | - | Tüm ödünçler (admin) |
| `/api/odunc/uye/{id}` | GET | - | Kullanıcının ödünçleri |
| `/api/uyeler/{id}` | GET | - | Kullanıcı profili |
| `/api/uyeler/{id}/profil` | PUT | `{Telefon, Email}` | Profil güncelle |
| `/api/kitaplar/{id}/puan` | GET | - | Kitap ortalama puanı |
| `/api/kitaplar/{id}/degerlendirmeler` | GET | - | Kitap yorumları |
| `/api/degerlendirmeler` | POST | `{KitapID, UyeID, Puan, Yorum}` | Yorum ekle |
| `/api/degerlendirmeler/{id}` | DELETE | - | Yorum sil |
| `/api/istatistikler` | GET | - | Dashboard istatistikleri |

## 13.11 Kullanım Kılavuzu

### 13.11.1 Web'den Erişim (PWA)

```bash
# 1. API'yi başlatın
cd api
dotnet run

# 2. Tarayıcıda açın
# http://localhost:5026/mobile/index.html
```

### 13.11.2 Mobil Cihazda (PWA)

1. Bilgisayarın IP adresini öğrenin: `ipconfig` (Windows)
2. Mobil cihazdan erişin: `http://192.168.x.x:5026/mobile/index.html`
3. Chrome menüsünden "Ana ekrana ekle" seçin
4. Uygulama gibi kullanmaya başlayın

### 13.11.3 Android Emülatör

```bash
# 1. API URL'sini emülatör için ayarlayın (api.js içinde)
const API_BASE = 'http://10.0.2.2:5026/api';

# 2. API'yi başlatın
cd api
dotnet run

# 3. Android Studio'da çalıştırın
cd mobile
npx cap open android
# → Run butonuna tıklayın
```

### 13.11.4 Gerçek Android Cihaz

```bash
# 1. API URL'sini bilgisayar IP'si olarak ayarlayın
const API_BASE = 'http://192.168.1.100:5026/api';

# 2. www klasörünü güncelleyin
cp js/api.js www/js/

# 3. Senkronize edin
npx cap sync android

# 4. USB debugging ile cihaza yükleyin
```

## 13.12 Admin Kullanıcı Özellikleri

Admin kullanıcısı mobil uygulamada aşağıdaki ek özelliklere sahiptir:

| Özellik | Açıklama |
|---------|----------|
| **Tüm Ödünçler** | Sadece kendi değil, tüm üyelerin ödünç işlemlerini görebilir |
| **Üye Adı Görünümü** | Ödünç kartlarında üye adı gösterilir |
| **Navigasyon Etiketi** | "Ödünçlerim" yerine "Ödünç İşlemleri" yazar |
| **Tüm Yorumları Silme** | Herhangi bir kullanıcının yorumunu silebilir |

## 13.13 Güvenlik Özellikleri

| Özellik | Uygulama |
|---------|----------|
| **JWT Token** | Her API isteğinde Bearer token gönderilir |
| **Token Saklama** | LocalStorage'da güvenli saklama |
| **Otomatik Çıkış** | 401 hatası alındığında otomatik logout |
| **Şifre Gizleme** | Password input type kullanımı |
| **Gmail Zorunluluğu** | Sadece @gmail.com adresleri kabul edilir |
| **E-posta Doğrulama** | 6 haneli kod ile doğrulama |
| **CSRF Koruması** | API tarafında token doğrulama |

## 13.14 Performans Optimizasyonları

| Optimizasyon | Açıklama |
|--------------|----------|
| **Paralel Veri Yükleme** | Promise.all ile eşzamanlı API çağrıları |
| **Debounce Arama** | 300ms bekleme ile gereksiz istekleri önleme |
| **Service Worker Cache** | Statik dosyalar önbellekte |
| **Lazy Loading** | Sayfalar gerektiğinde yüklenir |
| **Minimal Re-render** | Sadece değişen DOM güncellenir |
| **Font Preconnect** | Google Fonts hızlı yükleme |

## 13.15 Sorun Giderme

### 13.15.1 Yaygın Sorunlar

| Sorun | Çözüm |
|-------|-------|
| API bağlantı hatası | API'nin çalıştığını ve doğru URL kullandığınızı kontrol edin |
| Emülatörde bağlanmıyor | `10.0.2.2` IP adresini kullanın |
| Gerçek cihazda bağlanmıyor | Aynı Wi-Fi ağında olduğunuzdan emin olun |
| Token hatası | LocalStorage'ı temizleyip tekrar giriş yapın |
| Service Worker güncellenmedi | Önbelleği temizleyip sayfayı yenileyin |

### 13.15.2 Debug Modları

```javascript
// Konsol logları
console.log('Profil verisi:', profil);
console.log('deleteReview çağrıldı:', { reviewId, bookId });
console.log('Silme sonucu:', result);
```

## 13.16 Notlar ve Önemli Bilgiler

| Konu | Açıklama |
|------|----------|
| **API Adresi (Emülatör)** | `10.0.2.2:5026` - Android emülatörü için özel adres |
| **API Adresi (Gerçek Cihaz)** | Bilgisayarın yerel IP adresi (örn: 192.168.1.100:5026) |
| **Aynı Ağ Zorunluluğu** | Mobil cihaz ve bilgisayar aynı Wi-Fi'da olmalı |
| **HTTPS Gerekliliği** | PWA'nın tüm özellikleri için HTTPS gerekebilir (production) |
| **Offline Mod** | Service Worker sayesinde temel sayfalara çevrimdışı erişim |
| **Cleartext Traffic** | Development için HTTP izinli (capacitor.config.json) |
| **Minimum Android** | Android 5.1 (Lollipop) ve üzeri |

---

**Son Güncelleme:** 21 Aralık 2024  
**Dokümantasyon Sürümü:** 1.2
