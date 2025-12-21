# 📚 Kütüphane Otomasyon Sistemi

Modern ve kullanıcı dostu bir kütüphane yönetim sistemi. WPF masaüstü uygulaması ve REST API içerir.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Supabase-4169E1?logo=postgresql)
![License](https://img.shields.io/badge/License-Educational-green)

## 🚀 Özellikler

### 👤 Kullanıcı Sistemi
- **Yönetici Paneli:** Tüm işlemlere tam erişim
- **Üye Paneli:** Kitap görüntüleme, değerlendirme ve kişisel ödünç takibi
- Güvenli giriş sistemi (SHA256 şifreleme + JWT)
- Gmail ile şifremi unuttum özelliği
- E-posta doğrulama ile kayıt

### 📖 Kitap İşlemleri
- Kitap ekleme, düzenleme ve silme
- Toplu kitap silme (akıllı ödünç kontrolü)
- Excel'den içe/dışa aktarma
- Barkod tarama ile hızlı işlem
- ISBN-10 ve ISBN-13 doğrulama
- Kitap türü yönetimi
- Stok takibi
- Kitap değerlendirme ve yorum sistemi

### 👥 Üye İşlemleri
- Yeni üye kaydı (Gmail doğrulama)
- Üye bilgilerini güncelleme
- Üyeleri aktif/pasif yapma
- Akıllı silme (ilişkili kayıtları temizler)

### 📋 Ödünç İşlemleri
- Kitap ödünç verme
- İade alma
- Geciken kitapları takip etme
- Gecikme ücreti hesaplama
- Filtreleme (Tümü, Ödünçte, Geciken, İade Edilmiş)

### 📊 Raporlar ve İstatistikler
- Dashboard istatistikleri (gerçek zamanlı)
- Geciken kitaplar listesi
- Excel rapor çıktısı

### 🌐 REST API
- JWT Authentication ile güvenli erişim
- Kitaplar CRUD işlemleri
- Üyeler CRUD işlemleri
- Ödünç işlemleri
- Dashboard istatistikleri
- Swagger UI dokümantasyonu

## 🛠️ Teknolojiler

| Bileşen | Teknoloji |
|---------|-----------|
| Masaüstü App | .NET 8.0 WPF + Material Design |
| REST API | ASP.NET Core 8.0 Minimal API |
| Veritabanı | PostgreSQL (Supabase) |
| Authentication | JWT Bearer Token |
| Excel | ClosedXML |
| Barkod | ZXing.Net + AForge.Video |
| E-posta | MailKit (Gmail SMTP) |

## 🚀 Kurulum

### Gereksinimler
- .NET 8.0 SDK
- PostgreSQL veritabanı (veya Supabase hesabı)

### 1. API'yi Başlatma
```bash
cd api
dotnet run
```
API: http://localhost:5026
Swagger: http://localhost:5026/swagger

### 2. WPF Uygulamasını Başlatma
```bash
cd csharp
dotnet run
```

### Varsayılan Giriş Bilgileri
- **Kullanıcı Adı:** `admin`
- **Şifre:** `admin123`

## 📦 Dağıtım

### EXE Oluşturma (Tek Dosya)
```bash
cd csharp
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### API Dağıtımı
```bash
cd api
dotnet publish -c Release -o ./publish
```

## 📁 Proje Yapısı

```
kutuphane-otomasyonu/
├── api/                    # REST API projesi
│   ├── Program.cs          # API endpoint'leri (1600+ satır)
│   ├── Services/           # Email servisi
│   └── appsettings.json    # JWT yapılandırması
├── csharp/                 # WPF masaüstü uygulaması
│   ├── Views/              # Pencereler (Login, Register, Admin, Member)
│   ├── Pages/              # Admin sayfaları (Dashboard, Kitaplar, Üyeler, vb.)
│   ├── MemberPages/        # Üye sayfaları
│   ├── Assets/             # Logo ve görseller
│   ├── ApiService.cs       # API iletişim servisi
│   └── DatabaseHelper.cs   # Veritabanı yardımcı sınıfı
└── README.md
```

## 🔒 Güvenlik Özellikleri

- ✅ Şifreler SHA256 ile hashleniyor
- ✅ API istekleri JWT ile korunuyor
- ✅ Parametreli sorgular (SQL Injection koruması)
- ✅ ISBN doğrulama (ISBN-10, ISBN-13)
- ✅ E-posta doğrulama sistemi
- ✅ Güvenli şifre sıfırlama

## 📊 API Endpoint'leri

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | /api/giris | Giriş yap |
| POST | /api/kayit | Yeni kayıt |
| GET | /api/kitaplar | Kitap listesi |
| POST | /api/kitaplar | Kitap ekle |
| PUT | /api/kitaplar/{id} | Kitap güncelle |
| DELETE | /api/kitaplar/{id} | Kitap sil |
| DELETE | /api/kitaplar/toplu | Toplu silme |
| GET | /api/uyeler | Üye listesi |
| GET | /api/odunc | Ödünç listesi |
| POST | /api/odunc | Ödünç ver |
| PUT | /api/odunc/{id}/iade | İade al |
| GET | /api/odunc/stats | İstatistikler |
| GET | /api/istatistikler | Dashboard verileri |

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---


**Tarih:** Aralık 2025
