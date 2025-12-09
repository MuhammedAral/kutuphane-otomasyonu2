# Kütüphane Otomasyon Sistemi

Docker, MSSQL ve Python kullanarak geliştirilmiş modern bir kütüphane yönetim sistemi.

## 🚀 Özellikler

- Kitap yönetimi (Ekleme, güncelleme, silme, listeleme)
- Üye yönetimi
- Kitap ödünç alma/iade işlemleri
- Docker ile kolay kurulum
- RESTful API

## 📋 Gereksinimler

- Docker
- Docker Compose
- Git

## 🔧 Kurulum

1. **Repoyu klonlayın:**
```bash
git clone <repository-url>
cd kutuphane-otomasyonu
```

2. **Docker container'ları başlatın:**
```bash
docker-compose up -d
```

3. **Veritabanı otomatik olarak oluşturulacak ve hazır hale gelecektir.**

4. **Uygulamaya erişin:**
```
http://localhost:5000
```

## 📊 Veritabanı Yapısı

### Tablolar:
- **Kitaplar**: Kütüphanedeki kitap bilgileri
- **Uyeler**: Kütüphane üyeleri
- **OduncIslemleri**: Kitap ödünç alma/iade kayıtları

## 🔌 API Endpoints

### Kitaplar
- `GET /api/kitaplar` - Tüm kitapları listele
- `GET /api/kitaplar/<id>` - Belirli bir kitabı getir
- `POST /api/kitaplar` - Yeni kitap ekle
- `PUT /api/kitaplar/<id>` - Kitap güncelle
- `DELETE /api/kitaplar/<id>` - Kitap sil

### Üyeler
- `GET /api/uyeler` - Tüm üyeleri listele
- `GET /api/uyeler/<id>` - Belirli bir üyeyi getir
- `POST /api/uyeler` - Yeni üye ekle
- `PUT /api/uyeler/<id>` - Üye güncelle
- `DELETE /api/uyeler/<id>` - Üye sil

### Ödünç İşlemleri
- `GET /api/odunc` - Tüm ödünç işlemlerini listele
- `POST /api/odunc` - Yeni ödünç işlemi
- `PUT /api/odunc/<id>/iade` - Kitap iade et

## 📄 Lisans

MIT