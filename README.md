# 📚 Kütüphane Otomasyon Sistemi

Merhaba! Bu proje, kütüphanelerin günlük işlerini kolaylaştırmak için geliştirilmiş kapsamlı bir yazılım sistemidir. WPF masaüstü uygulaması ve REST API içerir.

## 🚀 Özellikler

### 👤 Kullanıcı Sistemi
- **Yönetici Paneli:** Tüm işlemlere erişim
- **Üye Paneli:** Kitap görüntüleme ve kişisel ödünç takibi
- Güvenli giriş sistemi (SHA256 şifreleme)
- Şifremi unuttum özelliği

### 📖 Kitap İşlemleri
- Yeni kitap ekleme
- Kitap düzenleme ve silme
- Kitap türü yönetimi
- Stok takibi

### 👥 Üye İşlemleri
- Yeni üye kaydı
- Üye bilgilerini güncelleme
- Üyeleri aktif/pasif yapma

### 📋 Ödünç İşlemleri
- Kitap ödünç verme
- İade alma
- Geciken kitapları takip etme

### 🌐 REST API
- Kitaplar CRUD işlemleri
- Üyeler CRUD işlemleri
- Ödünç işlemleri
- İstatistikler
- Swagger UI dokümantasyonu

## 🛠️ Teknolojiler

| Bileşen | Teknoloji |
|---------|-----------|
| Masaüstü App | .NET 8.0 WPF + Material Design |
| REST API | ASP.NET Core 8.0 Minimal API |
| Veritabanı | Microsoft SQL Server |
| Container | Docker |

## ⚙️ Kurulum

### 1. SQL Server Kurulumu (Docker)

```bash
docker-compose up -d
```

veya manuel:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Masaüstü Uygulamasını Çalıştırma

```bash
cd csharp
dotnet restore
dotnet run
```

### 3. REST API'yi Çalıştırma

```bash
cd api
dotnet restore
dotnet run
```

API Swagger UI: **http://localhost:5000/swagger**

## 🔐 Varsayılan Giriş Bilgileri

| Kullanıcı Adı | Şifre | Rol |
|---------------|-------|-----|
| admin | admin123 | Yönetici |

⚠️ **Önemli:** Üretime almadan önce admin şifresini değiştirmeyi unutmayın!

## 📡 API Endpoints

### Kitaplar
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/kitaplar` | Tüm kitapları listele |
| GET | `/api/kitaplar/{id}` | Kitap detayı |
| POST | `/api/kitaplar` | Yeni kitap ekle |
| PUT | `/api/kitaplar/{id}` | Kitap güncelle |
| DELETE | `/api/kitaplar/{id}` | Kitap sil |

### Üyeler
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/uyeler` | Tüm üyeleri listele |
| GET | `/api/uyeler/{id}` | Üye detayı |

### Ödünç İşlemleri
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/odunc` | Tüm işlemleri listele |
| POST | `/api/odunc` | Yeni ödünç ver |
| PUT | `/api/odunc/{id}/iade` | İade al |

### Diğer
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/turler` | Kitap türleri |
| GET | `/api/istatistikler` | Dashboard istatistikleri |

## 📁 Proje Yapısı

```
📁 kutuphane-otomasyonu
├── 📁 csharp                → WPF Masaüstü Uygulaması
│   ├── 📁 Views             → Giriş pencereleri
│   ├── 📁 Pages             → Yönetici panel sayfaları
│   ├── 📁 MemberPages       → Üye panel sayfaları
│   ├── 📁 Assets            → Logo ve görseller
│   ├── DatabaseHelper.cs    → Veritabanı işlemleri
│   └── App.xaml             → Uygulama başlangıç dosyası
├── 📁 api                   → ASP.NET Core REST API
│   └── Program.cs           → Minimal API endpoint'leri
├── docker-compose.yml       → SQL Server Docker yapılandırması
└── README.md                → Bu dosya
```

## 🤝 Katkıda Bulunma

1. Projeyi fork edin
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Yeni özellik eklendi'`)
4. Branch'i push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request açın

## 📄 Lisans

Bu proje açık kaynaklıdır ve özgürce kullanabilirsiniz.

---

Proje hakkında sorularınız varsa GitHub Issues üzerinden ulaşabilirsiniz. İyi kullanımlar! ✨
