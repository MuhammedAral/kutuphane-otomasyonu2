import customtkinter as ctk
from tkinter import ttk, messagebox
from datetime import datetime, timedelta

class OduncFrame(ctk.CTkFrame):
    def __init__(self, parent, db):
        super().__init__(parent)
        self.db = db
        
        # Başlık
        title = ctk.CTkLabel(self, text="🔄 Ödünç İşlemleri", font=ctk.CTkFont(size=24, weight="bold"))
        title.pack(pady=20)
        
        # Üst panel - İki sütun
        top_frame = ctk.CTkFrame(self)
        top_frame.pack(fill="x", padx=20, pady=10)
        
        # Sol: Yeni ödünç işlemi
        left_frame = ctk.CTkFrame(top_frame)
        left_frame.pack(side="left", fill="both", expand=True, padx=5)
        
        ctk.CTkLabel(left_frame, text="📤 Yeni Ödünç İşlemi", font=ctk.CTkFont(size=16, weight="bold")).pack(pady=10)
        
        # Kitap seçimi
        ctk.CTkLabel(left_frame, text="Kitap:").pack(pady=5)
        self.combo_kitap = ctk.CTkComboBox(left_frame, width=300, state="readonly")
        self.combo_kitap.pack(pady=5)
        
        # Üye seçimi
        ctk.CTkLabel(left_frame, text="Üye:").pack(pady=5)
        self.combo_uye = ctk.CTkComboBox(left_frame, width=300, state="readonly")
        self.combo_uye.pack(pady=5)
        
        # Ödünç ver butonu
        ctk.CTkButton(left_frame, text="📤 Ödünç Ver", command=self.add_odunc, width=200, height=40).pack(pady=20)
        
        # Sağ: İade işlemi
        right_frame = ctk.CTkFrame(top_frame)
        right_frame.pack(side="right", fill="both", expand=True, padx=5)
        
        ctk.CTkLabel(right_frame, text="📥 Kitap İade", font=ctk.CTkFont(size=16, weight="bold")).pack(pady=10)
        
        # İade edilecek işlem seçimi
        ctk.CTkLabel(right_frame, text="Ödünç İşlemi:").pack(pady=5)
        self.combo_islem = ctk.CTkComboBox(right_frame, width=300, state="readonly")
        self.combo_islem.pack(pady=5)
        
        # İade et butonu
        ctk.CTkButton(right_frame, text="📥 İade Et", command=self.iade_kitap, fg_color="green", width=200, height=40).pack(pady=20)
        
        # Yenile butonu
        ctk.CTkButton(top_frame, text="🔄 Listeleri Yenile", command=self.load_all_data, width=150).pack(side="bottom", pady=10)
        
        # Tablo
        table_frame = ctk.CTkFrame(self)
        table_frame.pack(fill="both", expand=True, padx=20, pady=10)
        
        # Filtre
        filter_frame = ctk.CTkFrame(table_frame, fg_color="transparent")
        filter_frame.pack(fill="x", pady=10)
        
        ctk.CTkLabel(filter_frame, text="Filtre:", font=ctk.CTkFont(size=14)).pack(side="left", padx=10)
        self.radio_var = ctk.StringVar(value="all")
        ctk.CTkRadioButton(filter_frame, text="Tümü", variable=self.radio_var, value="all", command=self.load_odunc).pack(side="left", padx=5)
        ctk.CTkRadioButton(filter_frame, text="Ödünçte", variable=self.radio_var, value="odunc", command=self.load_odunc).pack(side="left", padx=5)
        ctk.CTkRadioButton(filter_frame, text="İade Edildi", variable=self.radio_var, value="iade", command=self.load_odunc).pack(side="left", padx=5)
        ctk.CTkRadioButton(filter_frame, text="Gecikenler", variable=self.radio_var, value="geciken", command=self.load_odunc).pack(side="left", padx=5)
        
        # Treeview
        columns = ("ID", "Kitap", "Üye", "Ödünç Tarihi", "Beklenen İade", "İade Tarihi", "Durum", "Gecikme")
        self.tree = ttk.Treeview(table_frame, columns=columns, show="headings", height=12)
        
        for col in columns:
            self.tree.heading(col, text=col)
            if col == "ID":
                self.tree.column(col, width=50)
            elif col == "Gecikme":
                self.tree.column(col, width=80)
            else:
                self.tree.column(col, width=130)
        
        scrollbar = ttk.Scrollbar(table_frame, orient="vertical", command=self.tree.yview)
        self.tree.configure(yscrollcommand=scrollbar.set)
        
        self.tree.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")
        
        # Verileri yükle
        self.load_all_data()
    
    def load_all_data(self):
        """Tüm verileri yükle"""
        self.load_kitaplar()
        self.load_uyeler_combo()
        self.load_odunc_islemleri()
        self.load_odunc()
    
    def load_kitaplar(self):
        """Mevcut kitapları combo'ya yükle"""
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("SELECT KitapID, Baslik, Yazar, MevcutAdet FROM Kitaplar WHERE MevcutAdet > 0")
            rows = cursor.fetchall()
            
            kitaplar = [f"{row.KitapID} - {row.Baslik} ({row.Yazar}) - Mevcut: {row.MevcutAdet}" for row in rows]
            self.combo_kitap.configure(values=kitaplar)
            if kitaplar:
                self.combo_kitap.set(kitaplar[0])
        except Exception as e:
            messagebox.showerror("Hata", f"Kitaplar yüklenemedi: {str(e)}")
    
    def load_uyeler_combo(self):
        """Üyeleri combo'ya yükle"""
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("SELECT UyeID, AdSoyad FROM Uyeler WHERE AktifMi = 1")
            rows = cursor.fetchall()
            
            uyeler = [f"{row.UyeID} - {row.AdSoyad}" for row in rows]
            self.combo_uye.configure(values=uyeler)
            if uyeler:
                self.combo_uye.set(uyeler[0])
        except Exception as e:
            messagebox.showerror("Hata", f"Üyeler yüklenemedi: {str(e)}")
    
    def load_odunc_islemleri(self):
        """Ödünçte olan kitapları combo'ya yükle"""
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT o.IslemID, k.Baslik, u.AdSoyad 
                FROM OduncIslemleri o
                JOIN Kitaplar k ON o.KitapID = k.KitapID
                JOIN Uyeler u ON o.UyeID = u.UyeID
                WHERE o.Durum = 'Odunc'
            """)
            rows = cursor.fetchall()
            
            islemler = [f"{row.IslemID} - {row.Baslik} - {row.AdSoyad}" for row in rows]
            self.combo_islem.configure(values=islemler)
            if islemler:
                self.combo_islem.set(islemler[0])
        except Exception as e:
            messagebox.showerror("Hata", f"İşlemler yüklenemedi: {str(e)}")
    
    def load_odunc(self):
        """Ödünç işlemlerini yükle"""
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            
            filtre = self.radio_var.get()
            if filtre == "odunc":
                query = "WHERE o.Durum = 'Odunc'"
            elif filtre == "iade":
                query = "WHERE o.Durum = 'Iade'"
            elif filtre == "geciken":
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
            
            for row in rows:
                gecikme = ""
                if row.Durum == "Odunc" and row.BeklenenIadeTarihi < datetime.now():
                    gun = (datetime.now() - row.BeklenenIadeTarihi).days
                    gecikme = f"{gun} gün"
                
                # Satır rengi için tag
                tag = ""
                if gecikme:
                    tag = "geciken"
                
                self.tree.insert("", "end", values=(
                    row.IslemID,
                    row.Baslik,
                    row.AdSoyad,
                    row.OduncTarihi.strftime('%Y-%m-%d'),
                    row.BeklenenIadeTarihi.strftime('%Y-%m-%d'),
                    row.IadeTarihi.strftime('%Y-%m-%d') if row.IadeTarihi else "-",
                    row.Durum,
                    gecikme
                ), tags=(tag,))
            
            # Geciken satırları kırmızı yap
            self.tree.tag_configure("geciken", background="#ff6b6b")
            
        except Exception as e:
            messagebox.showerror("Hata", f"İşlemler yüklenemedi: {str(e)}")
    
    def add_odunc(self):
        """Yeni ödünç işlemi"""
        try:
            # Kitap ID'sini al
            kitap_text = self.combo_kitap.get()
            kitap_id = int(kitap_text.split(" - ")[0])
            
            # Üye ID'sini al
            uye_text = self.combo_uye.get()
            uye_id = int(uye_text.split(" - ")[0])
            
            cursor = self.db.get_connection().cursor()
            
            # Kitap mevcutluğunu kontrol et
            cursor.execute("SELECT MevcutAdet FROM Kitaplar WHERE KitapID = ?", kitap_id)
            row = cursor.fetchone()
            
            if not row or row.MevcutAdet <= 0:
                messagebox.showwarning("Uyarı", "Bu kitap mevcut değil!")
                return
            
            # Ödünç işlemini ekle
            beklenen_iade = datetime.now() + timedelta(days=14)
            cursor.execute("""
                INSERT INTO OduncIslemleri (KitapID, UyeID, BeklenenIadeTarihi)
                VALUES (?, ?, ?)
            """, kitap_id, uye_id, beklenen_iade)
            
            # Kitap mevcut adetini azalt
            cursor.execute("UPDATE Kitaplar SET MevcutAdet = MevcutAdet - 1 WHERE KitapID = ?", kitap_id)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Kitap başarıyla ödünç verildi!\n14 gün içinde iade edilmelidir.")
            self.load_all_data()
            
        except Exception as e:
            messagebox.showerror("Hata", f"Ödünç işlemi yapılamadı: {str(e)}")
    
    def iade_kitap(self):
        """Kitap iade et"""
        try:
            # İşlem ID'sini al
            islem_text = self.combo_islem.get()
            islem_id = int(islem_text.split(" - ")[0])
            
            cursor = self.db.get_connection().cursor()
            
            # İşlem bilgisini al
            cursor.execute("SELECT KitapID FROM OduncIslemleri WHERE IslemID = ?", islem_id)
            row = cursor.fetchone()
            
            if not row:
                messagebox.showwarning("Uyarı", "İşlem bulunamadı!")
                return
            
            # İade işlemini güncelle
            cursor.execute("""
                UPDATE OduncIslemleri 
                SET IadeTarihi = GETDATE(), Durum = 'Iade'
                WHERE IslemID = ?
            """, islem_id)
            
            # Kitap mevcut adetini artır
            cursor.execute("UPDATE Kitaplar SET MevcutAdet = MevcutAdet + 1 WHERE KitapID = ?", row.KitapID)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Kitap başarıyla iade edildi!")
            self.load_all_data()
            
        except Exception as e:
            messagebox.showerror("Hata", f"İade işlemi yapılamadı: {str(e)}")