import pyodbc
import time
import hashlib
import secrets
from datetime import datetime

class Database:
    def __init__(self):
        self.server = 'localhost'
        self.database = 'KutuphaneDB'
        self.username = 'sa'
        self.password = 'YourStrong@Password123'
        self.conn = None
        
    def connect(self):
        """Veritabanına bağlan"""
        max_retries = 5
        retry_count = 0
        
        while retry_count < max_retries:
            try:
                conn_string = (
                    f'DRIVER={{ODBC Driver 18 for SQL Server}};'
                    f'SERVER={self.server};'
                    f'DATABASE=master;'
                    f'UID={self.username};'
                    f'PWD={self.password};'
                    f'TrustServerCertificate=yes;'
                )
                self.conn = pyodbc.connect(conn_string, timeout=5)
                print("✅ Veritabanına bağlanıldı!")
                return True
            except Exception as e:
                retry_count += 1
                print(f"⚠️ Bağlantı denemesi {retry_count}/{max_retries} başarısız: {e}")
                if retry_count < max_retries:
                    time.sleep(2)
        
        raise Exception("❌ Veritabanına bağlanılamadı!")
    
    def create_database(self):
        """Veritabanını oluştur"""
        try:
            # CREATE DATABASE için autocommit gerekli
            self.conn.autocommit = True
            cursor = self.conn.cursor()
            
            # Önce veritabanı var mı kontrol et
            cursor.execute(f"SELECT name FROM sys.databases WHERE name = '{self.database}'")
            if not cursor.fetchone():
                cursor.execute(f"CREATE DATABASE {self.database}")
                print(f"✅ {self.database} veritabanı oluşturuldu!")
            else:
                print(f"✅ {self.database} veritabanı mevcut!")
            
            self.conn.autocommit = False
            self.conn.close()
            
            # Yeni veritabanına bağlan
            conn_string = (
                f'DRIVER={{ODBC Driver 18 for SQL Server}};'
                f'SERVER={self.server};'
                f'DATABASE={self.database};'
                f'UID={self.username};'
                f'PWD={self.password};'
                f'TrustServerCertificate=yes;'
            )
            self.conn = pyodbc.connect(conn_string)
            
        except Exception as e:
            print(f"❌ Veritabanı oluşturma hatası: {e}")
            raise
    
    def create_tables(self):
        """Tabloları oluştur"""
        try:
            cursor = self.conn.cursor()
            
            # Kullanıcılar tablosu (Yönetici ve Üyeler)
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Kullanicilar' AND xtype='U')
                CREATE TABLE Kullanicilar (
                    KullaniciID INT PRIMARY KEY IDENTITY(1,1),
                    KullaniciAdi NVARCHAR(50) UNIQUE NOT NULL,
                    Sifre NVARCHAR(255) NOT NULL,
                    AdSoyad NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(100) UNIQUE,
                    Telefon NVARCHAR(20),
                    Rol NVARCHAR(20) DEFAULT 'Uye',
                    AktifMi BIT DEFAULT 1,
                    OlusturmaTarihi DATETIME DEFAULT GETDATE(),
                    SonGirisTarihi DATETIME NULL,
                    SifreResetToken NVARCHAR(255) NULL,
                    TokenSonKullanmaTarihi DATETIME NULL
                )
            """)
            
            # Kitap Türleri tablosu
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='KitapTurleri' AND xtype='U')
                CREATE TABLE KitapTurleri (
                    TurID INT PRIMARY KEY IDENTITY(1,1),
                    TurAdi NVARCHAR(50) UNIQUE NOT NULL
                )
            """)
            
            # Kitaplar tablosu - Geliştirilmiş
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Kitaplar' AND xtype='U')
                CREATE TABLE Kitaplar (
                    KitapID INT PRIMARY KEY IDENTITY(1,1),
                    Baslik NVARCHAR(200) NOT NULL,
                    Yazar NVARCHAR(100) NOT NULL,
                    ISBN NVARCHAR(20) UNIQUE,
                    Barkod NVARCHAR(50) UNIQUE,
                    YayinYili INT,
                    TurID INT FOREIGN KEY REFERENCES KitapTurleri(TurID),
                    StokAdedi INT DEFAULT 0,
                    MevcutAdet INT DEFAULT 0,
                    RafNo NVARCHAR(20),
                    Aciklama NVARCHAR(500),
                    KapakResmi NVARCHAR(255),
                    EklenmeTarihi DATETIME DEFAULT GETDATE()
                )
            """)
            
            # Üyeler tablosu - Kaldırıldı, Kullanıcılar tablosu kullanılacak
            
            # Ödünç İşlemleri tablosu - Geliştirilmiş
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OduncIslemleri' AND xtype='U')
                CREATE TABLE OduncIslemleri (
                    IslemID INT PRIMARY KEY IDENTITY(1,1),
                    KitapID INT FOREIGN KEY REFERENCES Kitaplar(KitapID),
                    UyeID INT FOREIGN KEY REFERENCES Kullanicilar(KullaniciID),
                    OduncTarihi DATETIME DEFAULT GETDATE(),
                    IadeTarihi DATETIME NULL,
                    BeklenenIadeTarihi DATETIME NOT NULL,
                    Durum NVARCHAR(20) DEFAULT 'Odunc',
                    CezaTutari DECIMAL(10,2) DEFAULT 0,
                    CezaOdendi BIT DEFAULT 0,
                    Notlar NVARCHAR(255)
                )
            """)
            
            # Kitap İstekleri tablosu (Üyeler tarafından)
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='KitapIstekleri' AND xtype='U')
                CREATE TABLE KitapIstekleri (
                    IstekID INT PRIMARY KEY IDENTITY(1,1),
                    UyeID INT FOREIGN KEY REFERENCES Kullanicilar(KullaniciID),
                    KitapID INT FOREIGN KEY REFERENCES Kitaplar(KitapID) NULL,
                    KitapBaslik NVARCHAR(200),
                    Yazar NVARCHAR(100),
                    IstekTarihi DATETIME DEFAULT GETDATE(),
                    Durum NVARCHAR(20) DEFAULT 'Beklemede',
                    YoneticiNotu NVARCHAR(255),
                    Tip NVARCHAR(20) DEFAULT 'Odunc'
                )
            """)
            
            # Yeni Kitap Talepleri tablosu (Koleksiyonda olmayan kitaplar için)
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='YeniKitapTalepleri' AND xtype='U')
                CREATE TABLE YeniKitapTalepleri (
                    TalepID INT PRIMARY KEY IDENTITY(1,1),
                    UyeID INT FOREIGN KEY REFERENCES Kullanicilar(KullaniciID),
                    KitapBaslik NVARCHAR(200) NOT NULL,
                    Yazar NVARCHAR(100),
                    ISBN NVARCHAR(20),
                    TalepTarihi DATETIME DEFAULT GETDATE(),
                    Durum NVARCHAR(20) DEFAULT 'Beklemede',
                    YoneticiCevap NVARCHAR(255),
                    CevapTarihi DATETIME NULL
                )
            """)
            
            # Ayarlar tablosu (Ceza sistemi vb.)
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SistemAyarlari' AND xtype='U')
                CREATE TABLE SistemAyarlari (
                    AyarID INT PRIMARY KEY IDENTITY(1,1),
                    AyarAdi NVARCHAR(50) UNIQUE NOT NULL,
                    AyarDegeri NVARCHAR(255) NOT NULL,
                    Aciklama NVARCHAR(255)
                )
            """)
            
            # Bildirimler tablosu
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Bildirimler' AND xtype='U')
                CREATE TABLE Bildirimler (
                    BildirimID INT PRIMARY KEY IDENTITY(1,1),
                    KullaniciID INT FOREIGN KEY REFERENCES Kullanicilar(KullaniciID),
                    Baslik NVARCHAR(100) NOT NULL,
                    Mesaj NVARCHAR(500) NOT NULL,
                    Okundu BIT DEFAULT 0,
                    OlusturmaTarihi DATETIME DEFAULT GETDATE()
                )
            """)
            
            self.conn.commit()
            print("✅ Tablolar oluşturuldu!")
            
            # Varsayılan verileri ekle
            self.insert_default_data()
            
        except Exception as e:
            print(f"❌ Tablo oluşturma hatası: {e}")
            raise
    
    def insert_default_data(self):
        """Varsayılan verileri ekle"""
        try:
            cursor = self.conn.cursor()
            
            # Varsayılan yönetici hesabı
            cursor.execute("SELECT COUNT(*) FROM Kullanicilar WHERE Rol = 'Yonetici'")
            if cursor.fetchone()[0] == 0:
                admin_password = self.hash_password("admin123")
                cursor.execute("""
                    INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Rol)
                    VALUES (?, ?, ?, ?, ?)
                """, "admin", admin_password, "Sistem Yöneticisi", "admin@kutuphane.com", "Yonetici")
                print("✅ Varsayılan yönetici oluşturuldu (kullanıcı: admin, şifre: admin123)")
            
            # Varsayılan kitap türleri
            cursor.execute("SELECT COUNT(*) FROM KitapTurleri")
            if cursor.fetchone()[0] == 0:
                turler = ["Roman", "Bilim Kurgu", "Tarih", "Biyografi", "Şiir", "Çocuk", "Bilim", "Felsefe", "Psikoloji", "Diğer"]
                for tur in turler:
                    cursor.execute("INSERT INTO KitapTurleri (TurAdi) VALUES (?)", tur)
                print("✅ Varsayılan kitap türleri eklendi")
            
            # Varsayılan sistem ayarları
            cursor.execute("SELECT COUNT(*) FROM SistemAyarlari")
            if cursor.fetchone()[0] == 0:
                ayarlar = [
                    ("GunlukCezaTutari", "5", "Geciken her gün için ceza tutarı (TL)"),
                    ("MaxOduncSuresi", "14", "Maksimum ödünç alma süresi (gün)"),
                    ("MaxEsOdunc", "3", "Bir üyenin aynı anda alabileceği maksimum kitap sayısı"),
                    ("SMTPServer", "", "Email sunucusu (örn: smtp.gmail.com)"),
                    ("SMTPPort", "587", "Email sunucu portu"),
                    ("SMTPEmail", "", "Gönderici email adresi"),
                    ("SMTPPassword", "", "Email şifresi"),
                    ("SMSApiKey", "", "SMS API anahtarı")
                ]
                for ayar in ayarlar:
                    cursor.execute("""
                        INSERT INTO SistemAyarlari (AyarAdi, AyarDegeri, Aciklama)
                        VALUES (?, ?, ?)
                    """, ayar[0], ayar[1], ayar[2])
                print("✅ Varsayılan sistem ayarları eklendi")
            
            self.conn.commit()
            
        except Exception as e:
            print(f"⚠️ Varsayılan veri ekleme hatası: {e}")
    
    @staticmethod
    def hash_password(password):
        """Şifreyi hashle"""
        return hashlib.sha256(password.encode()).hexdigest()
    
    @staticmethod
    def generate_reset_token():
        """Şifre sıfırlama tokeni oluştur"""
        return secrets.token_urlsafe(32)
    
    def verify_login(self, kullanici_adi, sifre):
        """Kullanıcı girişini doğrula"""
        try:
            cursor = self.conn.cursor()
            hashed_password = self.hash_password(sifre)
            cursor.execute("""
                SELECT KullaniciID, KullaniciAdi, AdSoyad, Rol, Email, Telefon
                FROM Kullanicilar 
                WHERE KullaniciAdi = ? AND Sifre = ? AND AktifMi = 1
            """, kullanici_adi, hashed_password)
            
            row = cursor.fetchone()
            if row:
                # Son giriş tarihini güncelle
                cursor.execute("""
                    UPDATE Kullanicilar 
                    SET SonGirisTarihi = GETDATE() 
                    WHERE KullaniciID = ?
                """, row.KullaniciID)
                self.conn.commit()
                
                return {
                    'success': True,
                    'kullanici_id': row.KullaniciID,
                    'kullanici_adi': row.KullaniciAdi,
                    'ad_soyad': row.AdSoyad,
                    'rol': row.Rol,
                    'email': row.Email,
                    'telefon': row.Telefon
                }
            return {'success': False, 'message': 'Kullanıcı adı veya şifre hatalı!'}
        except Exception as e:
            return {'success': False, 'message': f'Hata: {str(e)}'}
    
    def check_username_exists(self, kullanici_adi):
        """Kullanıcı adının kullanılıp kullanılmadığını kontrol et"""
        try:
            cursor = self.conn.cursor()
            cursor.execute("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = ?", kullanici_adi)
            return cursor.fetchone()[0] > 0
        except:
            return False
    
    def get_connection(self):
        """Bağlantıyı döndür"""
        return self.conn
    
    def close(self):
        """Bağlantıyı kapat"""
        if self.conn:
            self.conn.close()
            print("✅ Veritabanı bağlantısı kapatıldı!")

def init_database():
    """Veritabanını başlat"""
    db = Database()
    db.connect()
    db.create_database()
    db.create_tables()
    return db