import pyodbc
import os
import time

class Database:
    def __init__(self):
        self.server = os.getenv('DB_SERVER', 'mssql')
        self.database = os.getenv('DB_NAME', 'KutuphaneDB')
        self.username = os.getenv('DB_USER', 'sa')
        self.password = os.getenv('DB_PASSWORD', 'YourStrong@Password123')
        self.conn = None
        
    def connect(self):
        max_retries = 30
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
                self.conn = pyodbc.connect(conn_string)
                print("Veritabanına bağlanıldı!")
                return True
            except Exception as e:
                retry_count += 1
                print(f"Bağlantı denemesi {retry_count}/{max_retries} başarısız: {e}")
                time.sleep(2)
        
        raise Exception("Veritabanına bağlanılamadı!")
    
    def create_database(self):
        try:
            cursor = self.conn.cursor()
            
            cursor.execute(f"""
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{self.database}')
                BEGIN
                    CREATE DATABASE {self.database}
                END
            """)
            self.conn.commit()
            print(f"{self.database} veritabanı hazır!")
            
            self.conn.close()
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
            print(f"Veritabanı oluşturma hatası: {e}")
            raise
    
    def create_tables(self):
        try:
            cursor = self.conn.cursor()
            
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Kitaplar' AND xtype='U')
                CREATE TABLE Kitaplar (
                    KitapID INT PRIMARY KEY IDENTITY(1,1),
                    Baslik NVARCHAR(200) NOT NULL,
                    Yazar NVARCHAR(100) NOT NULL,
                    ISBN NVARCHAR(20) UNIQUE,
                    YayinYili INT,
                    StokAdedi INT DEFAULT 0,
                    MevcutAdet INT DEFAULT 0,
                    EklenmeTarihi DATETIME DEFAULT GETDATE()
                )
            """)
            
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Uyeler' AND xtype='U')
                CREATE TABLE Uyeler (
                    UyeID INT PRIMARY KEY IDENTITY(1,1),
                    AdSoyad NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(100) UNIQUE,
                    Telefon NVARCHAR(20),
                    Adres NVARCHAR(200),
                    KayitTarihi DATETIME DEFAULT GETDATE(),
                    AktifMi BIT DEFAULT 1
                )
            """)
            
            cursor.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OduncIslemleri' AND xtype='U')
                CREATE TABLE OduncIslemleri (
                    IslemID INT PRIMARY KEY IDENTITY(1,1),
                    KitapID INT FOREIGN KEY REFERENCES Kitaplar(KitapID),
                    UyeID INT FOREIGN KEY REFERENCES Uyeler(UyeID),
                    OduncTarihi DATETIME DEFAULT GETDATE(),
                    IadeTarihi DATETIME NULL,
                    BeklenenIadeTarihi DATETIME NOT NULL,
                    Durum NVARCHAR(20) DEFAULT 'Odunc'
                )
            """)
            
            self.conn.commit()
            print("Tablolar oluşturuldu!")
            
        except Exception as e:
            print(f"Tablo oluşturma hatası: {e}")
            raise
    
    def get_connection(self):
        return self.conn
    
    def close(self):
        if self.conn:
            self.conn.close()
            print("Veritabanı bağlantısı kapatıldı!")

def init_database():
    db = Database()
    db.connect()
    db.create_database()
    db.create_tables()
    return db