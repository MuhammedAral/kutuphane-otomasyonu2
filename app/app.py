from flask import Flask, jsonify, request
from database import init_database
from datetime import datetime, timedelta

app = Flask(__name__)

db = None

@app.before_request
def before_request():
    global db
    if db is None:
        db = init_database()

@app.route('/')
def home():
    return jsonify({
        'mesaj': 'Kütüphane Otomasyon Sistemi API',
        'versiyon': '1.0',
        'endpoints': {
            'kitaplar': '/api/kitaplar',
            'uyeler': '/api/uyeler',
            'odunc': '/api/odunc'
        }
    })

@app.route('/api/kitaplar', methods=['GET'])
def get_kitaplar():
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("SELECT * FROM Kitaplar ORDER BY KitapID DESC")
        rows = cursor.fetchall()
        
        kitaplar = []
        for row in rows:
            kitaplar.append({
                'kitapID': row.KitapID,
                'baslik': row.Baslik,
                'yazar': row.Yazar,
                'isbn': row.ISBN,
                'yayinYili': row.YayinYili,
                'stokAdedi': row.StokAdedi,
                'mevcutAdet': row.MevcutAdet,
                'eklenmeTarihi': row.EklenmeTarihi.strftime('%Y-%m-%d %H:%M:%S')
            })
        
        return jsonify({'basarili': True, 'kitaplar': kitaplar})
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/kitaplar/<int:id>', methods=['GET'])
def get_kitap(id):
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("SELECT * FROM Kitaplar WHERE KitapID = ?", id)
        row = cursor.fetchone()
        
        if row:
            kitap = {
                'kitapID': row.KitapID,
                'baslik': row.Baslik,
                'yazar': row.Yazar,
                'isbn': row.ISBN,
                'yayinYili': row.YayinYili,
                'stokAdedi': row.StokAdedi,
                'mevcutAdet': row.MevcutAdet,
                'eklenmeTarihi': row.EklenmeTarihi.strftime('%Y-%m-%d %H:%M:%S')
            }
            return jsonify({'basarili': True, 'kitap': kitap})
        else:
            return jsonify({'basarili': False, 'hata': 'Kitap bulunamadı'}), 404
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/kitaplar', methods=['POST'])
def add_kitap():
    try:
        data = request.get_json()
        cursor = db.get_connection().cursor()
        
        cursor.execute("""
            INSERT INTO Kitaplar (Baslik, Yazar, ISBN, YayinYili, StokAdedi, MevcutAdet)
            VALUES (?, ?, ?, ?, ?, ?)
        """, data['baslik'], data['yazar'], data.get('isbn'), 
             data.get('yayinYili'), data.get('stokAdedi', 0), data.get('stokAdedi', 0))
        
        db.get_connection().commit()
        return jsonify({'basarili': True, 'mesaj': 'Kitap başarıyla eklendi'}), 201
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/kitaplar/<int:id>', methods=['PUT'])
def update_kitap(id):
    try:
        data = request.get_json()
        cursor = db.get_connection().cursor()
        
        cursor.execute("""
            UPDATE Kitaplar 
            SET Baslik = ?, Yazar = ?, ISBN = ?, YayinYili = ?, StokAdedi = ?
            WHERE KitapID = ?
        """, data['baslik'], data['yazar'], data.get('isbn'), 
             data.get('yayinYili'), data.get('stokAdedi'), id)
        
        db.get_connection().commit()
        return jsonify({'basarili': True, 'mesaj': 'Kitap başarıyla güncellendi'})
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/kitaplar/<int:id>', methods=['DELETE'])
def delete_kitap(id):
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("DELETE FROM Kitaplar WHERE KitapID = ?", id)
        db.get_connection().commit()
        
        return jsonify({'basarili': True, 'mesaj': 'Kitap başarıyla silindi'})
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/uyeler', methods=['GET'])
def get_uyeler():
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("SELECT * FROM Uyeler ORDER BY UyeID DESC")
        rows = cursor.fetchall()
        
        uyeler = []
        for row in rows:
            uyeler.append({
                'uyeID': row.UyeID,
                'adSoyad': row.AdSoyad,
                'email': row.Email,
                'telefon': row.Telefon,
                'adres': row.Adres,
                'kayitTarihi': row.KayitTarihi.strftime('%Y-%m-%d %H:%M:%S'),
                'aktifMi': row.AktifMi
            })
        
        return jsonify({'basarili': True, 'uyeler': uyeler})
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/uyeler', methods=['POST'])
def add_uye():
    try:
        data = request.get_json()
        cursor = db.get_connection().cursor()
        
        cursor.execute("""
            INSERT INTO Uyeler (AdSoyad, Email, Telefon, Adres)
            VALUES (?, ?, ?, ?)
        """, data['adSoyad'], data.get('email'), data.get('telefon'), data.get('adres'))
        
        db.get_connection().commit()
        return jsonify({'basarili': True, 'mesaj': 'Üye başarıyla eklendi'}), 201
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/odunc', methods=['GET'])
def get_odunc():
    try:
        cursor = db.get_connection().cursor()
        cursor.execute("""
            SELECT o.*, k.Baslik, u.AdSoyad 
            FROM OduncIslemleri o
            JOIN Kitaplar k ON o.KitapID = k.KitapID
            JOIN Uyeler u ON o.UyeID = u.UyeID
            ORDER BY o.IslemID DESC
        """)
        rows = cursor.fetchall()
        
        islemler = []
        for row in rows:
            islemler.append({
                'islemID': row.IslemID,
                'kitapID': row.KitapID,
                'kitapBaslik': row.Baslik,
                'uyeID': row.UyeID,
                'uyeAd': row.AdSoyad,
                'oduncTarihi': row.OduncTarihi.strftime('%Y-%m-%d %H:%M:%S'),
                'iadeTarihi': row.IadeTarihi.strftime('%Y-%m-%d %H:%M:%S') if row.IadeTarihi else None,
                'beklenenIadeTarihi': row.BeklenenIadeTarihi.strftime('%Y-%m-%d %H:%M:%S'),
                'durum': row.Durum
            })
        
        return jsonify({'basarili': True, 'islemler': islemler})
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/odunc', methods=['POST'])
def add_odunc():
    try:
        data = request.get_json()
        cursor = db.get_connection().cursor()
        
        cursor.execute("SELECT MevcutAdet FROM Kitaplar WHERE KitapID = ?", data['kitapID'])
        row = cursor.fetchone()
        
        if not row or row.MevcutAdet <= 0:
            return jsonify({'basarili': False, 'hata': 'Kitap mevcut değil'}), 400
        
        beklenen_iade = datetime.now() + timedelta(days=14)
        cursor.execute("""
            INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi)
            VALUES (?, ?, ?)
        """, data['kitapID'], data['uyeID'], beklenen_iade)
        
        cursor.execute("""
            UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = ?
        """, data['kitapID'])
        
        db.get_connection().commit()
        return jsonify({'basarili': True, 'mesaj': 'Ödünç işlemi başarıyla oluşturuldu'}), 201
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

@app.route('/api/odunc/<int:id>/iade', methods=['PUT'])
def iade_kitap(id):
    try:
        cursor = db.get_connection().cursor()
        
        cursor.execute("SELECT KitapID FROM OduncIslemleri WHERE IslemID = ?", id)
        row = cursor.fetchone()
        
        if not row:
            return jsonify({'basarili': False, 'hata': 'İşlem bulunamadı'}), 404
        
        cursor.execute("""
            UPDATE OduncIslemleri 
            SET IadeTarihi = GETDATE(), Durum = 'Iade'
            WHERE IslemID = ?
        """, id)
        
        cursor.execute("""
            UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 WHERE KitapID = ?
        """, row.KitapID)
        
        db.get_connection().commit()
        return jsonify({'basarili': True, 'mesaj': 'Kitap başarıyla iade edildi'})
    except Exception as e:
        return jsonify({'basarili': False, 'hata': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)