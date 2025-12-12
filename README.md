# 📚 Kütüphane Otomasyon Sistemi

Modern ve kullanıcı dostu bir kütüphane yönetim sistemi. WPF masaüstü uygulaması ve REST API içerir.

## 🚀 Özellikler

### 👤 Kullanıcı Sistemi
- **Yönetici Paneli:** Tüm işlemlere tam erişim
- **Üye Paneli:** Kitap görüntüleme, değerlendirme ve kişisel ödünç takibi
- Güvenli giriş sistemi (SHA256 şifreleme + JWT)
- Gmail ile şifremi unuttum özelliği
- E-posta doğrulama

### 📖 Kitap İşlemleri
- Kitap ekleme, düzenleme ve silme
- Toplu kitap silme (optimize edilmiş)
- Excel'den içe/dışa aktarma
- Barkod tarama ile hızlı işlem
- Kitap türü yönetimi
- Stok takibi
- Kitap değerlendirme ve yorum sistemi

### 👥 Üye İşlemleri
- Yeni üye kaydı
- Üye bilgilerini güncelleme
- Üyeleri aktif/pasif yapma

### 📋 Ödünç İşlemleri
- Kitap ödünç verme
- İade alma
- Geciken kitapları takip etme
- Gecikme ücreti hesaplama

### 📊 Raporlar
- Dashboard istatistikleri
- Geciken kitaplar listesi
- Excel rapor çıktısı

### 🌐 REST API
- JWT Authentication
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
| Barkod | ZXing.Net |

## 🚀 Kurulum

### Gereksinimler
- .NET 8.0 SDK
- PostgreSQL veritabanı (veya Supabase hesabı)

### API'yi Başlatma
```bash
cd api
dotnet run
```

### WPF Uygulamasını Başlatma
```bash
cd csharp
dotnet run
```

### Varsayılan Giriş Bilgileri
- **Kullanıcı Adı:** admin
- **Şifre:** admin123

## 📦 Dağıtım

### EXE Oluşturma
```bash
cd csharp
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

Oluşan `publish/KutuphaneOtomasyon.exe` dosyasını dağıtabilirsiniz.

## 📁 Proje Yapısı

```
kutuphane-otomasyonu/
├── api/                    # REST API projesi
│   ├── Program.cs          # API endpoint'leri
│   ├── Services/           # Email servisi
│   └── appsettings.json    # Yapılandırma
├── csharp/                 # WPF masaüstü uygulaması
│   ├── Views/              # Pencereler (Login, Register, vb.)
│   ├── Pages/              # Admin sayfaları
│   ├── MemberPages/        # Üye sayfaları
│   ├── Assets/             # Logo ve görseller
│   ├── ApiService.cs       # API iletişim servisi
│   └── DatabaseHelper.cs   # Veritabanı yardımcı sınıfı
└── README.md
```

## 🔒 Güvenlik

- Şifreler SHA256 ile hashleniyor
- API istekleri JWT ile korunuyor
- Parametreli sorgular (SQL Injection koruması)
- Connection pooling aktif

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---

**Geliştirici:** Muhammed Ali Aral
