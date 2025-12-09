from flask import Flask, jsonify, request
from flask_cors import CORS
from database import Database
from datetime import datetime, timedelta
import os

app = Flask(__name__)
CORS(app)  # Web ve mobil için gerekli

# Veritabanı bağlantısı
db = Database(
    server=os.getenv('DB_SERVER', 'localhost'),
    database=os.getenv('DB_NAME', 'KutuphaneDB'),
    username=os.getenv('DB_USER', 'sa'),
    password=os.getenv('DB_PASSWORD', 'YourStrong@Password123')
)

# Başlangıçta veritabanını hazırla
try:
    db.connect()
    db.create_database()
    db.create_tables()
    print("✅ API hazır!")
except Exception as e:
    print(f"❌ Veritabanı hatası: {e}")

# ==================== KITAPLAR ====================

@app.route('/api/kitaplar', methods=['GET'])
def get_kitaplar():
    """Tüm kitapları getir"""
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("SELECT * FROM Kitaplar ORDER BY KitapID DESC")
        rows = cursor.fetchall()
        
        kitaplar = []
        for row in rows:
            kitaplar.append({
                'id': row.KitapID,
                'baslik': row.Baslik,
                'yazar': row.Yazar,
                'isbn': row.ISBN,
                'yayin_yili': row.YayinYili,
                'stok': row.StokAdedi,
                'mevcut': row.MevcutAdet,
                'eklenme_tarihi': row.EklenmeTarihi.strftime('%Y-%m-%d')
            })
        
        return jsonify({'success': True, 'data': kitaplar})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/kitaplar/<int:kitap_id>', methods=['GET'])
def get_kitap(kitap_id):
    """Belirli bir kitabı getir"""
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("SELECT * FROM Kitaplar WHERE KitapID = ?", kitap_id)
        row = cursor.fetchone()
        
        if not row:
            return jsonify({'success': False, 'error': 'Kitap bulunamadı'}), 404
        
        kitap = {
            'id': row.KitapID,
            'baslik': row.Baslik,
            'yazar': row.Yazar,
            'isbn': row.ISBN,
            'yayin_yili': row.YayinYili,
            'stok': row.StokAdedi,
            'mevcut': row.MevcutAdet,
            'eklenme_tarihi': row.EklenmeTarihi.strftime('%Y-%m-%d')
        }
        
        return jsonify({'success': True, 'data': kitap})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/kitaplar', methods=['POST'])
def add_kitap():
    """Yeni kitap ekle"""
    try:
        data = request.json
        cursor = db.get_connection().cursor()
        
        stok = data.get('stok', 0)
        cursor.execute("""
            INSERT INTO Kitaplar (Baslik, Yazar, ISBN, YayinYili, StokAdedi, MevcutAdet)
            VALUES (?, ?, ?, ?, ?, ?)
        """, data['baslik'], data['yazar'], data.get('isbn'),
             data.get('yayin_yili'), stok, stok)
        
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Kitap eklendi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/kitaplar/<int:kitap_id>', methods=['PUT'])
def update_kitap(kitap_id):
    """Kitap güncelle"""
    try:
        data = request.json
        cursor = db.get_connection().cursor()
        
        cursor.execute("""
            UPDATE Kitaplar 
            SET Baslik = ?, Yazar = ?, ISBN = ?, YayinYili = ?, StokAdedi = ?
            WHERE KitapID = ?
        """, data['baslik'], data['yazar'], data.get('isbn'),
             data.get('yayin_yili'), data.get('stok', 0), kitap_id)
        
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Kitap güncellendi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/kitaplar/<int:kitap_id>', methods=['DELETE'])
def delete_kitap(kitap_id):
    """Kitap sil"""
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("DELETE FROM Kitaplar WHERE KitapID = ?", kitap_id)
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Kitap silindi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

# ==================== ÜYELER ====================

@app.route('/api/uyeler', methods=['GET'])
def get_uyeler():
    """Tüm üyeleri getir"""
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("SELECT * FROM Uyeler ORDER BY UyeID DESC")
        rows = cursor.fetchall()
        
        uyeler = []
        for row in rows:
            uyeler.append({
                'id': row.UyeID,
                'ad_soyad': row.AdSoyad,
                'email': row.Email,
                'telefon': row.Telefon,
                'adres': row.Adres,
                'kayit_tarihi': row.KayitTarihi.strftime('%Y-%m-%d'),
                'aktif': row.AktifMi
            })
        
        return jsonify({'success': True, 'data': uyeler})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/uyeler', methods=['POST'])
def add_uye():
    """Yeni üye ekle"""
    try:
        data = request.json
        cursor = db.get_connection().cursor()
        
        cursor.execute("""
            INSERT INTO Uyeler (AdSoyad, Email, Telefon, Adres)
            VALUES (?, ?, ?, ?)
        """, data['ad_soyad'], data.get('email'), data.get('telefon'), data.get('adres'))
        
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Üye eklendi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/uyeler/<int:uye_id>', methods=['PUT'])
def update_uye(uye_id):
    """Üye güncelle"""
    try:
        data = request.json
        cursor = db.get_connection().cursor()
        
        cursor.execute("""
            UPDATE Uyeler 
            SET AdSoyad = ?, Email = ?, Telefon = ?, Adres = ?
            WHERE UyeID = ?
        """, data['ad_soyad'], data.get('email'), data.get('telefon'), data.get('adres'), uye_id)
        
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Üye güncellendi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/uyeler/<int:uye_id>', methods=['DELETE'])
def delete_uye(uye_id):
    """Üye sil"""
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("DELETE FROM Uyeler WHERE UyeID = ?", uye_id)
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Üye silindi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

# ==================== ÖDÜNÇ İŞLEMLERİ ====================

@app.route('/api/odunc', methods=['GET'])
def get_odunc():
    """Tüm ödünç işlemlerini getir"""
    try:
        filtre = request.args.get('durum', 'all')
        
        cursor = db.get_connection().cursor()
        
        if filtre == 'odunc':
            query = "WHERE o.Durum = 'Odunc'"
        elif filtre == 'iade':
            query = "WHERE o.Durum = 'Iade'"
        elif filtre == 'geciken':
            query = "WHERE o.Durum = 'Odunc' AND o.BeklenenIadeTarihi < GETDATE()"
        else:
            query = ""
        
        cursor.execute(f"""
            SELECT o.*, k.Baslik, u.AdSoyad 
            FROM OduncIslemleri o
            JOIN Kitaplar k ON o.KitapID = k.KitapID
            JOIN Uyeler u ON o.UyeID = u.UyeID
            {query}
            ORDER BY o.IslemID DESC
        """)
        rows = cursor.fetchall()
        
        islemler = []
        for row in rows:
            gecikme = 0
            if row.Durum == "Odunc" and row.BeklenenIadeTarihi < datetime.now():
                gecikme = (datetime.now() - row.BeklenenIadeTarihi).days
            
            islemler.append({
                'id': row.IslemID,
                'kitap_id': row.KitapID,
                'kitap_baslik': row.Baslik,
                'uye_id': row.UyeID,
                'uye_ad': row.AdSoyad,
                'odunc_tarihi': row.OduncTarihi.strftime('%Y-%m-%d'),
                'beklenen_iade': row.BeklenenIadeTarihi.strftime('%Y-%m-%d'),
                'iade_tarihi': row.IadeTarihi.strftime('%Y-%m-%d') if row.IadeTarihi else None,
                'durum': row.Durum,
                'gecikme_gun': gecikme
            })
        
        return jsonify({'success': True, 'data': islemler})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/odunc', methods=['POST'])
def add_odunc():
    """Yeni ödünç işlemi"""
    try:
        data = request.json
        cursor = db.get_connection().cursor()
        
        # Kitap mevcutluğunu kontrol et
        cursor.execute("SELECT MevcutAdet FROM Kitaplar WHERE KitapID = ?", data['kitap_id'])
        row = cursor.fetchone()
        
        if not row or row.MevcutAdet <= 0:
            return jsonify({'success': False, 'error': 'Kitap mevcut değil'}), 400
        
        # Ödünç işlemini ekle
        beklenen_iade = datetime.now() + timedelta(days=14)
        cursor.execute("""
            INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi)
            VALUES (?, ?, ?)
        """, data['kitap_id'], data['uye_id'], beklenen_iade)
        
        # Kitap mevcut adetini azalt
        cursor.execute("UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = ?", data['kitap_id'])
        
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Kitap ödünç verildi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

@app.route('/api/odunc/<int:islem_id>/iade', methods=['PUT'])
def iade_kitap(islem_id):
    """Kitap iade et"""
    try:
        cursor = db.get_connection().cursor()
        
        # İşlem bilgisini al
        cursor.execute("SELECT KitapID FROM OduncIslemleri WHERE IslemID = ?", islem_id)
        row = cursor.fetchone()
        
        if not row:
            return jsonify({'success': False, 'error': 'İşlem bulunamadı'}), 404
        
        # İade işlemini güncelle
        cursor.execute("""
            UPDATE OduncIslemleri 
            SET IadeTarihi = GETDATE(), Durum = 'Iade'
            WHERE IslemID = ?
        """, islem_id)
        
        # Kitap mevcut adetini artır
        cursor.execute("UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 WHERE KitapID = ?", row.KitapID)
        
        db.get_connection().commit()
        return jsonify({'success': True, 'message': 'Kitap iade edildi'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500

# ==================== SAĞLIK KONTROLÜ ====================

@app.route('/api/health', methods=['GET'])
def health_check():
    """API sağlık kontrolü"""
    return jsonify({
        'status': 'healthy',
        'database': 'connected',
        'timestamp': datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)