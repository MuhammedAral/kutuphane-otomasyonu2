# 📚 Kütüphane Otomasyon Sistemi

Merhaba! Bu proje, kütüphanelerin günlük işlerini kolaylaştırmak için geliştirilmiş bir yazılım. Kitap takibi, üye yönetimi ve ödünç işlemleri gibi temel ihtiyaçları karşılıyor.

## Ne İşe Yarar?

Bir kütüphane düşünün: Raflarınızda yüzlerce kitap var, onlarca üyeniz kitap alıp iade ediyor. Bunların hepsini kağıt kalemle takip etmek hem zor hem de hata yapma riski yüksek. İşte bu uygulama tam da bu sorunu çözüyor.

**Masaüstü uygulaması** ile bilgisayarınızdan tüm işlemleri yapabilirsiniz. Ayrıca **REST API** sayesinde ileride mobil uygulama veya web sitesi de ekleyebilirsiniz.

## Neler Yapabilirsiniz?

### Kitap İşlemleri
- Yeni kitap ekleyebilirsiniz
- Mevcut kitapları düzenleyebilir veya silebilirsiniz
- Barkod okuyucu ile hızlıca kitap tarayabilirsiniz
- Excel dosyasından toplu kitap aktarabilirsiniz

### Üye İşlemleri
- Yeni üye kaydı oluşturabilirsiniz
- Üye bilgilerini güncelleyebilirsiniz
- Hangi üyede hangi kitap var görebilirsiniz

### Ödünç İşlemleri
- Kitap ödünç verebilirsiniz
- İade alabilirsiniz
- Geciken kitapları takip edebilirsiniz

## Nasıl Kurulur?

### 1. Öncelikle Docker'ı başlatın
Veritabanı için SQL Server kullanıyoruz. Docker ile çok kolay:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Masaüstü uygulamasını çalıştırın
```bash
cd desktop
pip install -r requirements.txt
python main.py
```

### 3. API'yi başlatın (isteğe bağlı)
```bash
cd api
pip install -r requirements.txt
uvicorn main:app --reload
```

## Giriş Bilgileri

İlk açılışta kullanabileceğiniz hazır bir yönetici hesabı var:

- **Kullanıcı adı:** admin
- **Şifre:** admin123

## API Kullanımı

API'yi başlattıktan sonra tarayıcınızda şu adresi açın:

👉 http://localhost:8000/docs

Burada tüm API endpoint'lerini görebilir ve test edebilirsiniz.

## Proje Yapısı

```
📁 kutuphane-otomasyonu
├── 📁 desktop          → Masaüstü uygulaması
│   ├── main.py         → Ana uygulama dosyası
│   ├── database.py     → Veritabanı işlemleri
│   └── assets          → Logo ve görseller
├── 📁 api              → REST API
│   └── main.py         → API endpoint'leri
└── README.md           → Bu dosya
```

## Yardım ve Destek

Bir sorunla karşılaşırsanız veya öneriniz varsa GitHub üzerinden issue açabilirsiniz.

## Lisans

Bu proje açık kaynaklıdır ve özgürce kullanabilirsiniz.

---

Proje hakkında sorularınız varsa bana ulaşabilirsiniz. İyi kullanımlar! ✨
