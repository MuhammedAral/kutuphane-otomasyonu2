# 📱 Kütüphane Mobil Uygulaması

Modern, PWA (Progressive Web App) tabanlı kütüphane otomasyon mobil uygulaması.

## 🚀 Özellikler

### 🔐 Giriş Sistemi
- Kullanıcı adı ve şifre ile giriş
- Oturum yönetimi (JWT Token)
- Otomatik yönlendirme

### 📚 Kitaplar
- Tüm kitapları listeleme
- Arama (kitap adı, yazar, ISBN)
- Türe göre filtreleme
- Kitap detayları
- Değerlendirme ve yorum yapma
- Yıldız bazlı puanlama (1-5)

### 📋 Ödünçlerim
- Aktif ödünç işlemleri
- Geçmiş ödünç kayıtları
- Gecikme uyarıları
- Kalan gün gösterimi

### 👤 Profil
- Kullanıcı bilgileri
- Ad Soyad, E-posta, Telefon
- Kayıt tarihi
- Çıkış yapma

## 🛠 Teknolojiler

- **HTML5** - Yapı
- **CSS3** - Modern tasarım
  - CSS Variables
  - Flexbox & Grid
  - Gradients & Animations
  - Dark Theme
- **JavaScript ES6+** - Uygulama mantığı
- **PWA** - Progressive Web App
  - Service Worker
  - Offline destek
  - Ana ekrana ekleme

## 📲 Kullanım

### Web'den Erişim
1. API'yi çalıştırın: `dotnet run` (api klasöründe)
2. Tarayıcıda açın: `http://localhost:5000/mobile/index.html`

### Mobil Cihazda
1. Yukarıdaki URL'yi telefonun tarayıcısında açın
2. Menüden "Ana Ekrana Ekle" seçeneğini kullanın
3. Artık uygulama gibi kullanabilirsiniz!

## 🎨 Tasarım Özellikleri

- **Dark Theme** - Göz yormayan koyu tema
- **Gradient Renkler** - Modern renk geçişleri
- **Smooth Animations** - Akıcı animasyonlar
- **Mobile-First** - Mobil öncelikli tasarım
- **Safe Area** - Çentikli telefonlarla uyumlu
- **Touch-Optimized** - Dokunmaya optimize

## 📁 Dosya Yapısı

```
mobile/
├── index.html          # Ana HTML
├── manifest.json       # PWA manifest
├── sw.js              # Service Worker
├── css/
│   └── mobile.css     # Stiller
├── js/
│   ├── api.js         # API iletişimi
│   └── app.js         # Uygulama mantığı
└── icons/
    └── icon.svg       # Uygulama ikonu
```

## 🔗 API Endpoint'leri

Uygulama şu API endpoint'lerini kullanır:

| Endpoint | Açıklama |
|----------|----------|
| `POST /api/giris` | Kullanıcı girişi |
| `GET /api/kitaplar` | Kitap listesi |
| `GET /api/turler` | Kitap türleri |
| `GET /api/odunc/uye/{id}` | Kullanıcının ödünç kayıtları |
| `GET /api/uyeler/{id}` | Kullanıcı profili |
| `GET /api/kitaplar/{id}/puan` | Kitap puanı |
| `GET /api/kitaplar/{id}/degerlendirmeler` | Kitap yorumları |
| `POST /api/degerlendirmeler` | Yorum ekle |
| `DELETE /api/degerlendirmeler/{id}` | Yorum sil |

## 🔒 Varsayılan Giriş Bilgileri

- **Kullanıcı Adı:** admin
- **Şifre:** admin123

## 📝 Notlar

- API localhost:5000 portunda çalışmalıdır
- Mobil cihazdan erişim için aynı ağda olmalısınız
- PWA özelliklerini kullanmak için HTTPS gerekebilir
