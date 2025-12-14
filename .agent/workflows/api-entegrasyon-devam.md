---
description: API entegrasyonu ve iyileştirmeler
---

# API Entegrasyon Durumu

## ✅ Tamamlanan Özellikler

### Admin Paneli
- Login / Register / Password Reset
- Dashboard (stats, geciken-kitaplar)
- Kitaplar (Liste, Ekle, Düzenle, Sil, Toplu Sil, Toplu Ekle)
- Ödünç İşlemleri (Liste, İstatistik, Yeni Ödünç, İade)
- Raporlar (detaylı istatistikler)
- Üyeler (Liste, Ekle, Sil)
- OduncDialog - API tabanlı ✅
- UyeDialog - API tabanlı ✅
- Excel Import - API tabanlı ✅

### Üye Paneli  
- AnasayfaPage - API tabanlı ✅
- KitaplarViewPage - API tabanlı ✅
- OdunclerimPage - API tabanlı ✅
- ProfilPage - API tabanlı ✅

### Güvenlik
- JWT Authentication aktif ✅
- Token bazlı yetkilendirme ✅
- Sadece Login/Register/PasswordReset anonim ✅

## 📝 API Endpoint'leri

### Giriş (AllowAnonymous)
- `POST /api/giris`
- `POST /api/auth/register`
- `POST /api/auth/sifremi-unuttum`
- `POST /api/auth/sifre-sifirla`
- `POST /api/auth/verify-email`

### Kitaplar (RequireAuthorization)
- `GET /api/kitaplar`
- `POST /api/kitaplar`
- `PUT /api/kitaplar/{id}`
- `DELETE /api/kitaplar/{id}`
- `POST /api/kitaplar/toplu`

### Üyeler (RequireAuthorization)
- `GET /api/uyeler`
- `POST /api/uyeler`
- `DELETE /api/uyeler/{id}`

### Üye Paneli (RequireAuthorization)
- `GET /api/uye/{uyeId}/stats`
- `GET /api/uye/{uyeId}/son-islemler`
- `GET /api/uye/{uyeId}/oduncler`
- `GET /api/uye/{uyeId}/profil`
- `PUT /api/uye/{uyeId}/profil`

### Ödünç (RequireAuthorization)
- `GET /api/odunc`
- `POST /api/odunc`
- `PUT /api/odunc/{id}/iade`

### Diğer (RequireAuthorization)
- `GET /api/turler`
- `GET /api/istatistikler`
- `GET /api/raporlar`

## 📝 Notlar
- API: http://localhost:5026
- Swagger: http://localhost:5026/swagger
- Veritabanı: Supabase PostgreSQL

## 🔧 Başlatma
```bash
# API
cd api && dotnet run

# C# App
cd csharp && dotnet run
```
