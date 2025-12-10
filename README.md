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

