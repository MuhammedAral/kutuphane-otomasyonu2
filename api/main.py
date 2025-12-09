"""
Kütüphane Otomasyon Sistemi - REST API
FastAPI ile modern ve hızlı API
"""

from fastapi import FastAPI, HTTPException, Depends, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import OAuth2PasswordBearer, OAuth2PasswordRequestForm
from jose import JWTError, jwt
from passlib.context import CryptContext
from datetime import datetime, timedelta
from typing import Optional, List
from pydantic import BaseModel, EmailStr
import pyodbc
import os

# ==================== YAPILANDIRMA ====================

# JWT Ayarları
SECRET_KEY = "kutuphane-super-secret-key-2024-change-in-production"
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 60 * 24  # 24 saat

# Veritabanı Ayarları
DB_SERVER = os.getenv("DB_SERVER", "localhost")
DB_DATABASE = os.getenv("DB_DATABASE", "KutuphaneDB")
DB_USERNAME = os.getenv("DB_USERNAME", "sa")
DB_PASSWORD = os.getenv("DB_PASSWORD", "YourStrong@Password123")

# ==================== PYDANTIC MODELLER ====================

class Token(BaseModel):
    access_token: str
    token_type: str

class TokenData(BaseModel):
    kullanici_adi: Optional[str] = None
    rol: Optional[str] = None

class KullaniciBase(BaseModel):
    kullanici_adi: str
    ad_soyad: str
    email: Optional[EmailStr] = None
    telefon: Optional[str] = None

class KullaniciCreate(KullaniciBase):
    sifre: str

class KullaniciResponse(KullaniciBase):
    kullanici_id: int
    rol: str
    aktif_mi: bool
    olusturma_tarihi: datetime

    class Config:
        from_attributes = True

class KitapBase(BaseModel):
    baslik: str
    yazar: str
    isbn: Optional[str] = None
    barkod: Optional[str] = None
    yayin_yili: Optional[int] = None
    tur_id: Optional[int] = None
    stok_adedi: int = 1
    raf_no: Optional[str] = None
    aciklama: Optional[str] = None

class KitapCreate(KitapBase):
    pass

class KitapUpdate(BaseModel):
    baslik: Optional[str] = None
    yazar: Optional[str] = None
    isbn: Optional[str] = None
    barkod: Optional[str] = None
    yayin_yili: Optional[int] = None
    tur_id: Optional[int] = None
    stok_adedi: Optional[int] = None
    raf_no: Optional[str] = None
    aciklama: Optional[str] = None

class KitapResponse(KitapBase):
    kitap_id: int
    mevcut_adet: int
    tur_adi: Optional[str] = None
    eklenme_tarihi: datetime

    class Config:
        from_attributes = True

class OduncCreate(BaseModel):
    kitap_id: int
    uye_id: int

class OduncResponse(BaseModel):
    islem_id: int
    kitap_id: int
    kitap_baslik: str
    uye_id: int
    uye_adi: str
    odunc_tarihi: datetime
    beklenen_iade_tarihi: datetime
    iade_tarihi: Optional[datetime] = None
    durum: str
    gecikme_gun: int = 0

    class Config:
        from_attributes = True

class KitapTuruResponse(BaseModel):
    tur_id: int
    tur_adi: str

# ==================== VERİTABANI ====================

def get_db_connection():
    """Veritabanı bağlantısı oluştur"""
    conn_string = (
        f'DRIVER={{ODBC Driver 18 for SQL Server}};'
        f'SERVER={DB_SERVER};'
        f'DATABASE={DB_DATABASE};'
        f'UID={DB_USERNAME};'
        f'PWD={DB_PASSWORD};'
        f'TrustServerCertificate=yes;'
    )
    return pyodbc.connect(conn_string)

# ==================== GÜVENLİK ====================

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="auth/login")

def verify_password(plain_password, hashed_password):
    """Şifre doğrulama (hashlib ile uyumlu)"""
    import hashlib
    return hashlib.sha256(plain_password.encode()).hexdigest() == hashed_password

def get_password_hash(password):
    """Şifre hashleme"""
    import hashlib
    return hashlib.sha256(password.encode()).hexdigest()

def create_access_token(data: dict, expires_delta: Optional[timedelta] = None):
    """JWT token oluştur"""
    to_encode = data.copy()
    expire = datetime.utcnow() + (expires_delta or timedelta(minutes=15))
    to_encode.update({"exp": expire})
    return jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)

async def get_current_user(token: str = Depends(oauth2_scheme)):
    """Mevcut kullanıcıyı al"""
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Kimlik doğrulama başarısız",
        headers={"WWW-Authenticate": "Bearer"},
    )
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        kullanici_adi: str = payload.get("sub")
        rol: str = payload.get("rol")
        if kullanici_adi is None:
            raise credentials_exception
        token_data = TokenData(kullanici_adi=kullanici_adi, rol=rol)
    except JWTError:
        raise credentials_exception
    
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute(
        "SELECT * FROM Kullanicilar WHERE KullaniciAdi = ? AND AktifMi = 1",
        token_data.kullanici_adi
    )
    user = cursor.fetchone()
    conn.close()
    
    if user is None:
        raise credentials_exception
    return user

async def get_admin_user(current_user = Depends(get_current_user)):
    """Sadece admin kullanıcıları"""
    if current_user.Rol != "Yonetici":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Bu işlem için yetkiniz yok"
        )
    return current_user

# ==================== FASTAPI UYGULAMA ====================

app = FastAPI(
    title="📚 Kütüphane Otomasyon API",
    description="Kütüphane Otomasyon Sistemi için RESTful API",
    version="2.0.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

# CORS ayarları
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ==================== AUTH ENDPOINTS ====================

@app.post("/auth/login", response_model=Token, tags=["🔐 Kimlik Doğrulama"])
async def login(form_data: OAuth2PasswordRequestForm = Depends()):
    """Kullanıcı girişi - JWT token al"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    hashed_password = get_password_hash(form_data.password)
    cursor.execute(
        "SELECT * FROM Kullanicilar WHERE KullaniciAdi = ? AND Sifre = ? AND AktifMi = 1",
        form_data.username, hashed_password
    )
    user = cursor.fetchone()
    conn.close()
    
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Kullanıcı adı veya şifre hatalı",
            headers={"WWW-Authenticate": "Bearer"},
        )
    
    access_token = create_access_token(
        data={"sub": user.KullaniciAdi, "rol": user.Rol},
        expires_delta=timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    )
    return {"access_token": access_token, "token_type": "bearer"}

@app.get("/auth/me", response_model=KullaniciResponse, tags=["🔐 Kimlik Doğrulama"])
async def get_me(current_user = Depends(get_current_user)):
    """Mevcut kullanıcı bilgilerini al"""
    return KullaniciResponse(
        kullanici_id=current_user.KullaniciID,
        kullanici_adi=current_user.KullaniciAdi,
        ad_soyad=current_user.AdSoyad,
        email=current_user.Email,
        telefon=current_user.Telefon,
        rol=current_user.Rol,
        aktif_mi=current_user.AktifMi,
        olusturma_tarihi=current_user.OlusturmaTarihi
    )

# ==================== KİTAP ENDPOINTS ====================

@app.get("/kitaplar", response_model=List[KitapResponse], tags=["📚 Kitaplar"])
async def get_kitaplar(
    arama: Optional[str] = None,
    tur_id: Optional[int] = None,
    limit: int = 100,
    offset: int = 0
):
    """Tüm kitapları listele"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    query = """
        SELECT k.*, kt.TurAdi 
        FROM Kitaplar k
        LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
        WHERE 1=1
    """
    params = []
    
    if arama:
        query += " AND (k.Baslik LIKE ? OR k.Yazar LIKE ? OR k.Barkod LIKE ?)"
        params.extend([f"%{arama}%", f"%{arama}%", f"%{arama}%"])
    
    if tur_id:
        query += " AND k.TurID = ?"
        params.append(tur_id)
    
    query += " ORDER BY k.KitapID DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY"
    params.extend([offset, limit])
    
    cursor.execute(query, params)
    rows = cursor.fetchall()
    conn.close()
    
    return [
        KitapResponse(
            kitap_id=row.KitapID,
            baslik=row.Baslik,
            yazar=row.Yazar,
            isbn=row.ISBN,
            barkod=row.Barkod,
            yayin_yili=row.YayinYili,
            tur_id=row.TurID,
            tur_adi=row.TurAdi,
            stok_adedi=row.StokAdedi,
            mevcut_adet=row.MevcutAdet,
            raf_no=row.RafNo,
            aciklama=row.Aciklama,
            eklenme_tarihi=row.EklenmeTarihi
        ) for row in rows
    ]

@app.get("/kitaplar/{kitap_id}", response_model=KitapResponse, tags=["📚 Kitaplar"])
async def get_kitap(kitap_id: int):
    """Tek bir kitabı getir"""
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT k.*, kt.TurAdi 
        FROM Kitaplar k
        LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
        WHERE k.KitapID = ?
    """, kitap_id)
    row = cursor.fetchone()
    conn.close()
    
    if not row:
        raise HTTPException(status_code=404, detail="Kitap bulunamadı")
    
    return KitapResponse(
        kitap_id=row.KitapID,
        baslik=row.Baslik,
        yazar=row.Yazar,
        isbn=row.ISBN,
        barkod=row.Barkod,
        yayin_yili=row.YayinYili,
        tur_id=row.TurID,
        tur_adi=row.TurAdi,
        stok_adedi=row.StokAdedi,
        mevcut_adet=row.MevcutAdet,
        raf_no=row.RafNo,
        aciklama=row.Aciklama,
        eklenme_tarihi=row.EklenmeTarihi
    )

@app.post("/kitaplar", response_model=KitapResponse, tags=["📚 Kitaplar"])
async def create_kitap(kitap: KitapCreate, current_user = Depends(get_admin_user)):
    """Yeni kitap ekle (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    cursor.execute("""
        INSERT INTO Kitaplar (Baslik, Yazar, ISBN, Barkod, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo, Aciklama)
        OUTPUT INSERTED.KitapID
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, kitap.baslik, kitap.yazar, kitap.isbn, kitap.barkod, 
         kitap.yayin_yili, kitap.tur_id, kitap.stok_adedi, kitap.stok_adedi,
         kitap.raf_no, kitap.aciklama)
    
    kitap_id = cursor.fetchone()[0]
    conn.commit()
    conn.close()
    
    return await get_kitap(kitap_id)

@app.put("/kitaplar/{kitap_id}", response_model=KitapResponse, tags=["📚 Kitaplar"])
async def update_kitap(kitap_id: int, kitap: KitapUpdate, current_user = Depends(get_admin_user)):
    """Kitap güncelle (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    # Mevcut kitabı kontrol et
    cursor.execute("SELECT * FROM Kitaplar WHERE KitapID = ?", kitap_id)
    if not cursor.fetchone():
        conn.close()
        raise HTTPException(status_code=404, detail="Kitap bulunamadı")
    
    update_fields = []
    params = []
    
    if kitap.baslik: update_fields.append("Baslik = ?"); params.append(kitap.baslik)
    if kitap.yazar: update_fields.append("Yazar = ?"); params.append(kitap.yazar)
    if kitap.isbn is not None: update_fields.append("ISBN = ?"); params.append(kitap.isbn)
    if kitap.barkod is not None: update_fields.append("Barkod = ?"); params.append(kitap.barkod)
    if kitap.yayin_yili is not None: update_fields.append("YayinYili = ?"); params.append(kitap.yayin_yili)
    if kitap.tur_id is not None: update_fields.append("TurID = ?"); params.append(kitap.tur_id)
    if kitap.stok_adedi is not None: update_fields.append("StokAdedi = ?"); params.append(kitap.stok_adedi)
    if kitap.raf_no is not None: update_fields.append("RafNo = ?"); params.append(kitap.raf_no)
    if kitap.aciklama is not None: update_fields.append("Aciklama = ?"); params.append(kitap.aciklama)
    
    if update_fields:
        params.append(kitap_id)
        cursor.execute(f"UPDATE Kitaplar SET {', '.join(update_fields)} WHERE KitapID = ?", params)
        conn.commit()
    
    conn.close()
    return await get_kitap(kitap_id)

@app.delete("/kitaplar/{kitap_id}", tags=["📚 Kitaplar"])
async def delete_kitap(kitap_id: int, current_user = Depends(get_admin_user)):
    """Kitap sil (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    cursor.execute("SELECT * FROM Kitaplar WHERE KitapID = ?", kitap_id)
    if not cursor.fetchone():
        conn.close()
        raise HTTPException(status_code=404, detail="Kitap bulunamadı")
    
    cursor.execute("DELETE FROM Kitaplar WHERE KitapID = ?", kitap_id)
    conn.commit()
    conn.close()
    
    return {"message": "Kitap başarıyla silindi"}

# ==================== ÖDÜNÇ ENDPOINTS ====================

@app.get("/odunc", response_model=List[OduncResponse], tags=["🔄 Ödünç İşlemleri"])
async def get_odunc_islemleri(
    durum: Optional[str] = None,
    current_user = Depends(get_current_user)
):
    """Ödünç işlemlerini listele"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    query = """
        SELECT o.*, k.Baslik, u.AdSoyad
        FROM OduncIslemleri o
        JOIN Kitaplar k ON o.KitapID = k.KitapID
        JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
        WHERE 1=1
    """
    params = []
    
    if durum:
        query += " AND o.Durum = ?"
        params.append(durum)
    
    query += " ORDER BY o.IslemID DESC"
    
    cursor.execute(query, params)
    rows = cursor.fetchall()
    conn.close()
    
    result = []
    for row in rows:
        gecikme = 0
        if row.Durum == "Odunc" and row.BeklenenIadeTarihi < datetime.now():
            gecikme = (datetime.now() - row.BeklenenIadeTarihi).days
        
        result.append(OduncResponse(
            islem_id=row.IslemID,
            kitap_id=row.KitapID,
            kitap_baslik=row.Baslik,
            uye_id=row.UyeID,
            uye_adi=row.AdSoyad,
            odunc_tarihi=row.OduncTarihi,
            beklenen_iade_tarihi=row.BeklenenIadeTarihi,
            iade_tarihi=row.IadeTarihi,
            durum=row.Durum,
            gecikme_gun=gecikme
        ))
    
    return result

@app.post("/odunc", response_model=OduncResponse, tags=["🔄 Ödünç İşlemleri"])
async def create_odunc(odunc: OduncCreate, current_user = Depends(get_admin_user)):
    """Yeni ödünç işlemi (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    # Kitap mevcutluğunu kontrol et
    cursor.execute("SELECT MevcutAdet FROM Kitaplar WHERE KitapID = ?", odunc.kitap_id)
    kitap = cursor.fetchone()
    if not kitap or kitap.MevcutAdet <= 0:
        conn.close()
        raise HTTPException(status_code=400, detail="Kitap mevcut değil")
    
    # Ödünç işlemi ekle
    beklenen_iade = datetime.now() + timedelta(days=14)
    cursor.execute("""
        INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi)
        OUTPUT INSERTED.IslemID
        VALUES (?, ?, ?)
    """, odunc.kitap_id, odunc.uye_id, beklenen_iade)
    
    islem_id = cursor.fetchone()[0]
    
    # Kitap mevcut adetini azalt
    cursor.execute("UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = ?", odunc.kitap_id)
    conn.commit()
    
    # Oluşturulan kaydı getir
    cursor.execute("""
        SELECT o.*, k.Baslik, u.AdSoyad
        FROM OduncIslemleri o
        JOIN Kitaplar k ON o.KitapID = k.KitapID
        JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
        WHERE o.IslemID = ?
    """, islem_id)
    row = cursor.fetchone()
    conn.close()
    
    return OduncResponse(
        islem_id=row.IslemID,
        kitap_id=row.KitapID,
        kitap_baslik=row.Baslik,
        uye_id=row.UyeID,
        uye_adi=row.AdSoyad,
        odunc_tarihi=row.OduncTarihi,
        beklenen_iade_tarihi=row.BeklenenIadeTarihi,
        iade_tarihi=row.IadeTarihi,
        durum=row.Durum,
        gecikme_gun=0
    )

@app.post("/odunc/{islem_id}/iade", tags=["🔄 Ödünç İşlemleri"])
async def iade_kitap(islem_id: int, current_user = Depends(get_admin_user)):
    """Kitap iade et (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    cursor.execute("SELECT KitapID, Durum FROM OduncIslemleri WHERE IslemID = ?", islem_id)
    islem = cursor.fetchone()
    
    if not islem:
        conn.close()
        raise HTTPException(status_code=404, detail="İşlem bulunamadı")
    
    if islem.Durum == "Iade":
        conn.close()
        raise HTTPException(status_code=400, detail="Bu kitap zaten iade edilmiş")
    
    # İade işlemi
    cursor.execute("""
        UPDATE OduncIslemleri 
        SET IadeTarihi = GETDATE(), Durum = 'Iade'
        WHERE IslemID = ?
    """, islem_id)
    
    # Kitap mevcut adetini artır
    cursor.execute("UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 WHERE KitapID = ?", islem.KitapID)
    conn.commit()
    conn.close()
    
    return {"message": "Kitap başarıyla iade edildi"}

# ==================== ÜYELER ENDPOINTS ====================

@app.get("/uyeler", response_model=List[KullaniciResponse], tags=["👥 Üyeler"])
async def get_uyeler(current_user = Depends(get_admin_user)):
    """Tüm üyeleri listele (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM Kullanicilar WHERE Rol = 'Uye' ORDER BY KullaniciID DESC")
    rows = cursor.fetchall()
    conn.close()
    
    return [
        KullaniciResponse(
            kullanici_id=row.KullaniciID,
            kullanici_adi=row.KullaniciAdi,
            ad_soyad=row.AdSoyad,
            email=row.Email,
            telefon=row.Telefon,
            rol=row.Rol,
            aktif_mi=row.AktifMi,
            olusturma_tarihi=row.OlusturmaTarihi
        ) for row in rows
    ]

@app.post("/uyeler", response_model=KullaniciResponse, tags=["👥 Üyeler"])
async def create_uye(uye: KullaniciCreate, current_user = Depends(get_admin_user)):
    """Yeni üye ekle (Sadece Admin)"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    # Kullanıcı adı kontrolü
    cursor.execute("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = ?", uye.kullanici_adi)
    if cursor.fetchone()[0] > 0:
        conn.close()
        raise HTTPException(status_code=400, detail="Bu kullanıcı adı zaten kullanılıyor")
    
    hashed_password = get_password_hash(uye.sifre)
    
    cursor.execute("""
        INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol)
        OUTPUT INSERTED.KullaniciID
        VALUES (?, ?, ?, ?, ?, 'Uye')
    """, uye.kullanici_adi, hashed_password, uye.ad_soyad, uye.email, uye.telefon)
    
    kullanici_id = cursor.fetchone()[0]
    conn.commit()
    
    cursor.execute("SELECT * FROM Kullanicilar WHERE KullaniciID = ?", kullanici_id)
    row = cursor.fetchone()
    conn.close()
    
    return KullaniciResponse(
        kullanici_id=row.KullaniciID,
        kullanici_adi=row.KullaniciAdi,
        ad_soyad=row.AdSoyad,
        email=row.Email,
        telefon=row.Telefon,
        rol=row.Rol,
        aktif_mi=row.AktifMi,
        olusturma_tarihi=row.OlusturmaTarihi
    )

# ==================== KİTAP TÜRLERİ ENDPOINTS ====================

@app.get("/turler", response_model=List[KitapTuruResponse], tags=["📂 Kitap Türleri"])
async def get_turler():
    """Kitap türlerini listele"""
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT TurID, TurAdi FROM KitapTurleri ORDER BY TurAdi")
    rows = cursor.fetchall()
    conn.close()
    
    return [KitapTuruResponse(tur_id=row.TurID, tur_adi=row.TurAdi) for row in rows]

# ==================== İSTATİSTİKLER ====================

@app.get("/istatistikler", tags=["📊 İstatistikler"])
async def get_istatistikler(current_user = Depends(get_current_user)):
    """Genel istatistikleri getir"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    # Toplam kitap
    cursor.execute("SELECT SUM(StokAdedi) FROM Kitaplar")
    toplam_kitap = cursor.fetchone()[0] or 0
    
    # Ödünçte olan
    cursor.execute("SELECT COUNT(*) FROM OduncIslemleri WHERE Durum = 'Odunc'")
    oduncte = cursor.fetchone()[0] or 0
    
    # Aktif üyeler
    cursor.execute("SELECT COUNT(*) FROM Kullanicilar WHERE AktifMi = 1 AND Rol = 'Uye'")
    aktif_uyeler = cursor.fetchone()[0] or 0
    
    # Geciken kitaplar
    cursor.execute("""
        SELECT COUNT(*) FROM OduncIslemleri 
        WHERE Durum = 'Odunc' AND BeklenenIadeTarihi < GETDATE()
    """)
    geciken = cursor.fetchone()[0] or 0
    
    conn.close()
    
    return {
        "toplam_kitap": toplam_kitap,
        "oduncte_olan": oduncte,
        "aktif_uyeler": aktif_uyeler,
        "geciken_kitaplar": geciken
    }

# ==================== ANA SAYFA ====================

@app.get("/", tags=["🏠 Ana Sayfa"])
async def root():
    """API Ana Sayfa"""
    return {
        "mesaj": "📚 Kütüphane Otomasyon API'sine Hoş Geldiniz!",
        "versiyon": "2.0.0",
        "dokumantasyon": "/docs",
        "redoc": "/redoc"
    }

# ==================== ÇALIŞTIRMA ====================

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000, reload=True)
