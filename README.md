# Kütüphane Otomasyon Sistemi

📚 Modern ve kullanıcı dostu kütüphane yönetim sistemi. Masaüstü uygulaması ve REST API ile tam özellikli.

<<<<<<< HEAD
## ✨ Özellikler

### 📱 Masaüstü Uygulaması
- **Modern Arayüz:** CustomTkinter ile karanlık tema destekli şık tasarım
- **Kitap Yönetimi:** Ekleme, düzenleme, silme, arama
- **Üye Yönetimi:** Üye kayıt ve takip sistemi
- **Ödünç İşlemleri:** Kitap ödünç verme ve iade takibi
- **Barkod Tarama:** Kamera ile barkod okuma desteği
- **Excel Entegrasyonu:** Kitapları Excel'den/e aktarma
- **Gecikme Takibi:** Geciken kitapların otomatik tespiti

### 🌐 REST API
- **FastAPI:** Hızlı ve modern Python API framework
- **JWT Kimlik Doğrulama:** Güvenli token tabanlı yetkilendirme
- **Swagger Dokümantasyonu:** Otomatik API dokümantasyonu
- **CORS Desteği:** Web uygulamaları için hazır

## 🛠️ Teknolojiler

| Bileşen | Teknoloji |
|---------|-----------|
| Masaüstü | Python, CustomTkinter |
| API | FastAPI, Uvicorn |
| Veritabanı | SQL Server (Docker) |
| Kimlik Doğrulama | JWT, SHA-256 |

## 📦 Kurulum

### Gereksinimler
- Python 3.10+
- Docker Desktop
- ODBC Driver 18 for SQL Server

### 1. Veritabanını Başlatın
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Masaüstü Uygulaması
```bash
cd desktop
pip install -r requirements.txt
python main.py
```

### 3. API
```bash
cd api
pip install -r requirements.txt
uvicorn main:app --reload
```

## 🔐 Varsayılan Giriş Bilgileri

| Kullanıcı Adı | Şifre | Rol |
|---------------|-------|-----|
| admin | admin123 | Yönetici |

## 📡 API Endpoints

| Endpoint | Metod | Açıklama |
|----------|-------|----------|
| `/auth/login` | POST | Giriş yap |
| `/kitaplar` | GET/POST | Kitap listele/ekle |
| `/kitaplar/{id}` | GET/PUT/DELETE | Kitap detay/güncelle/sil |
| `/odunc` | GET/POST | Ödünç işlemleri |
| `/odunc/{id}/iade` | POST | Kitap iade |
| `/uyeler` | GET/POST | Üye listele/ekle |
| `/turler` | GET | Kitap türleri |
| `/istatistikler` | GET | İstatistikler |

**API Dokümantasyonu:** http://localhost:8000/docs

## 📸 Ekran Görüntüleri

### Masaüstü Uygulaması
- Modern karanlık tema
- Sezgisel sidebar menüsü
- Tablo görünümleri

### API Swagger
- İnteraktif API dokümantasyonu
- Test arayüzü

## 📁 Proje Yapısı

```
kutuphane-otomasyonu/
├── desktop/                 # Masaüstü uygulaması
│   ├── main.py             # Ana uygulama
│   ├── database.py         # Veritabanı işlemleri
│   ├── ui_kitaplar_enhanced.py
│   ├── ui_uyeler.py
│   ├── ui_odunc.py
│   ├── ui_dashboard.py
│   ├── assets/             # Logo ve görseller
│   └── requirements.txt
├── api/                    # REST API
│   ├── main.py            # FastAPI uygulaması
│   └── requirements.txt
└── README.md
```

## 🚀 Gelecek Özellikler

- [ ] Mobil uygulama
- [ ] Email/SMS bildirimleri
- [ ] Raporlama sistemi
- [ ] QR kod desteği
- [ ] Çoklu dil desteği

## 👨‍💻 Geliştirici

**Muhammed Ali Aral**

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

---

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!
=======
>>>>>>> 51571c7633d04c762d27542e054dacfa43523820
