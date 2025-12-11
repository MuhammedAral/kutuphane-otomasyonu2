import random
import os
import sys

def generate_data():
    data = []
    # 26 books (20-30 range requested)
    books = [
        ("Sessizliğin Sesi", "Ahmet Yılmaz"), ("Geleceğin Kodları", "Mehmet Demir"), ("Yapay Zeka Çağı", "Ayşe Kaya"),
        ("Veri Bilimine Giriş", "Fatma Çelik"), ("Derin Öğrenme Sanatı", "Mustafa Şahin"), ("Bulut Bilişim", "Zeynep Yıldız"),
        ("Siber Güvenlik Temelleri", "Ali Öztürk"), ("Blockchain Devrimi", "Emine Arslan"), ("Kuantum Dünyası", "Hüseyin Polat"),
        ("Nesnelerin İnterneti", "Hatice Koç"), ("Büyük Veri Analizi", "İbrahim Aslan"), ("Robotik Kodlama", "Elif Doğan"),
        ("Oyun Geliştirme", "Murat Ünal"), ("Mobil Uygulama Tasarımı", "Selin Kara"), ("Web Programlama", "Burak Can"),
        ("İleri C# Teknikleri", "Ceren Aksoy"), ("Python ile Veri", "Deniz Kurt"), ("Algoritma Mantığı", "Ece Güneş"),
        ("Veritabanı Yönetimi", "Fırat Tekin"), ("Ağ Güvenliği", "Gamze Yıldırım"), ("Yazılım Mimarisi", "Hakan Bulut"),
        ("Mikroservisler", "İrem Sönmez"), ("DevOps Kültürü", "Kaan Çetin"), ("Agile Metodolojileri", "Leyla Ertekin"),
        ("Test Otomasyonu", "Mert Yavuz"), ("Linux Sistem Yönetimi", "Nazlı Şen")
    ]
    
    for title, author in books:
        data.append({
            "Baslik": title,
            "Yazar": author,
            "ISBN": "978" + str(random.randint(1000000000, 9999999999)), # 13 digit barcode/ISBN
            "YayinYili": random.randint(1990, 2024),
            "TurID": random.randint(1, 5),
            "StokAdedi": random.randint(5, 50),
            "RafNo": f"{random.choice(['A','B','C','D'])}-{random.randint(1, 20)}"
        })
    return data

try:
    import pandas as pd
    data = generate_data()
    df = pd.DataFrame(data)
    
    output_file = "YeniKitaplar.xlsx"
    # Try using openpyxl directly via pandas
    try:
        df.to_excel(output_file, index=False)
        print(f"XLSX created at {os.path.abspath(output_file)}")
    except ImportError:
        # Fallback if openpyxl is missing but pandas is present
        print("openpyxl missing, trying csv")
        df.to_csv("YeniKitaplar.csv", index=False, sep=";")
        print(f"CSV created at {os.path.abspath('YeniKitaplar.csv')}")
        
except ImportError:
    print("Pandas not found. Generating CSV manually.")
    data = generate_data()
    import csv
    with open("YeniKitaplar.csv", "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=data[0].keys(), delimiter=";")
        writer.writeheader()
        writer.writerows(data)
    print(f"CSV created at {os.path.abspath('YeniKitaplar.csv')}")
