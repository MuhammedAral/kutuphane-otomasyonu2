import customtkinter as ctk
from tkinter import ttk, messagebox
from datetime import datetime, timedelta

class OduncFrame(ctk.CTkFrame):
    def __init__(self, parent, db):
        super().__init__(parent)
        self.db = db
        
        # Dark tema için Treeview stili
        self.setup_treeview_style()
        
        # Başlık ve aksiyonlar
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=20)
        
        title = ctk.CTkLabel(
            header,
            text="🔄 Ödünç İşlemleri",
            font=ctk.CTkFont(size=24, weight="bold")
        )
        title.pack(side="left")
        
        # Sağ taraf butonlar
        action_frame = ctk.CTkFrame(header, fg_color="transparent")
        action_frame.pack(side="right")
        
        ctk.CTkButton(
            action_frame,
            text="🔄 Yenile",
            command=self.load_all_data,
            width=100,
            height=35
        ).pack(side="left", padx=5)
        
        # Form ve tablo için container
        content = ctk.CTkFrame(self, fg_color="transparent")
        content.pack(fill="both", expand=True, padx=20, pady=(0, 20))
        
        # Form alanı (üstte) - İki panel
        form_container = ctk.CTkFrame(content, fg_color="transparent")
        form_container.pack(fill="x", pady=(0, 15))
        
        # Sol: Yeni ödünç işlemi
        left_frame = ctk.CTkFrame(form_container)
        left_frame.pack(side="left", fill="both", expand=True, padx=(0, 10))
        self.create_odunc_form(left_frame)
        
        # Sağ: İade işlemi
        right_frame = ctk.CTkFrame(form_container)
        right_frame.pack(side="right", fill="both", expand=True, padx=(10, 0))
        self.create_iade_form(right_frame)
        
        # Tablo alanı (altta)
        table_frame = ctk.CTkFrame(content)
        table_frame.pack(fill="both", expand=True)
        
        self.create_table(table_frame)
        
        # Verileri yükle
        self.load_all_data()
    
    def setup_treeview_style(self):
        """Treeview için dark tema stili"""
        style = ttk.Style()
        style.theme_use("clam")
        
        # Treeview dark tema
        style.configure("Dark.Treeview",
            background="#2b2b2b",
            foreground="white",
            fieldbackground="#2b2b2b",
            borderwidth=0,
            rowheight=30
        )
        style.configure("Dark.Treeview.Heading",
            background="#1f1f1f",
            foreground="white",
            borderwidth=1,
            font=('Segoe UI', 10, 'bold')
        )
        style.map("Dark.Treeview",
            background=[("selected", "#3b82f6")],
            foreground=[("selected", "white")]
        )
        style.map("Dark.Treeview.Heading",
            background=[("active", "#333333")]
        )
    
    def create_odunc_form(self, parent):
        """Ödünç işlemi formunu oluştur"""
        # Form başlığı
        header = ctk.CTkFrame(parent, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=15)
        
        ctk.CTkLabel(
            header,
            text="📤 Yeni Ödünç İşlemi",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(side="left")
        
        # Form içeriği
        form_content = ctk.CTkFrame(parent, fg_color="transparent")
        form_content.pack(fill="x", padx=20, pady=(0, 15))
        
        # Kitap seçimi
        ctk.CTkLabel(form_content, text="Kitap *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.combo_kitap = ctk.CTkComboBox(form_content, height=35, state="readonly")
        self.combo_kitap.pack(fill="x", pady=(0, 10))
        
        # Üye seçimi
        ctk.CTkLabel(form_content, text="Üye *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.combo_uye = ctk.CTkComboBox(form_content, height=35, state="readonly")
        self.combo_uye.pack(fill="x", pady=(0, 10))
        
        # Ödünç ver butonu
        ctk.CTkButton(
            parent,
            text="📤 Ödünç Ver",
            command=self.add_odunc,
            height=40,
            fg_color="#10b981"
        ).pack(padx=20, pady=(10, 20), fill="x")
    
    def create_iade_form(self, parent):
        """İade işlemi formunu oluştur"""
        # Form başlığı
        header = ctk.CTkFrame(parent, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=15)
        
        ctk.CTkLabel(
            header,
            text="📥 Kitap İade",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(side="left")
        
        # Form içeriği
        form_content = ctk.CTkFrame(parent, fg_color="transparent")
        form_content.pack(fill="x", padx=20, pady=(0, 15))
        
        # İade edilecek işlem seçimi
        ctk.CTkLabel(form_content, text="Ödünç İşlemi *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.combo_islem = ctk.CTkComboBox(form_content, height=35, state="readonly")
        self.combo_islem.pack(fill="x", pady=(0, 10))
        
        # Boşluk için placeholder
        ctk.CTkLabel(form_content, text="", height=35).pack(pady=(5, 10))
        
        # İade et butonu
        ctk.CTkButton(
            parent,
            text="📥 İade Et",
            command=self.iade_kitap,
            height=40,
            fg_color="#3b82f6"
        ).pack(padx=20, pady=(10, 20), fill="x")
    
    def create_table(self, parent):
        """Tablo alanını oluştur"""
        # Filtre
        filter_frame = ctk.CTkFrame(parent, fg_color="transparent")
        filter_frame.pack(fill="x", padx=15, pady=15)
        
        ctk.CTkLabel(
            filter_frame,
            text="🔍 Filtre:",
            font=ctk.CTkFont(size=14, weight="bold")
        ).pack(side="left", padx=(0, 10))
        
        self.radio_var = ctk.StringVar(value="all")
        
        ctk.CTkRadioButton(
            filter_frame,
            text="Tümü",
            variable=self.radio_var,
            value="all",
            command=self.load_odunc
        ).pack(side="left", padx=10)
        
        ctk.CTkRadioButton(
            filter_frame,
            text="Ödünçte",
            variable=self.radio_var,
            value="odunc",
            command=self.load_odunc
        ).pack(side="left", padx=10)
        
        ctk.CTkRadioButton(
            filter_frame,
            text="İade Edildi",
            variable=self.radio_var,
            value="iade",
            command=self.load_odunc
        ).pack(side="left", padx=10)
        
        ctk.CTkRadioButton(
            filter_frame,
            text="⚠️ Gecikenler",
            variable=self.radio_var,
            value="geciken",
            command=self.load_odunc
        ).pack(side="left", padx=10)
        
        # Treeview
        tree_frame = ctk.CTkFrame(parent, fg_color="#2b2b2b")
        tree_frame.pack(fill="both", expand=True, padx=15, pady=(0, 15))
        
        columns = ("ID", "Kitap", "Üye", "Ödünç Tarihi", "Beklenen İade", "İade Tarihi", "Durum", "Gecikme")
        self.tree = ttk.Treeview(tree_frame, columns=columns, show="headings", height=12, style="Dark.Treeview")
        
        # Sütun başlıkları ve genişlikler
        column_widths = {
            "ID": 50,
            "Kitap": 180,
            "Üye": 150,
            "Ödünç Tarihi": 100,
            "Beklenen İade": 100,
            "İade Tarihi": 100,
            "Durum": 80,
            "Gecikme": 80
        }
        
        for col in columns:
            self.tree.heading(col, text=col)
            self.tree.column(col, width=column_widths.get(col, 100))
        
        # Scrollbar
        scrollbar_y = ttk.Scrollbar(tree_frame, orient="vertical", command=self.tree.yview)
        scrollbar_x = ttk.Scrollbar(tree_frame, orient="horizontal", command=self.tree.xview)
        self.tree.configure(yscrollcommand=scrollbar_y.set, xscrollcommand=scrollbar_x.set)
        
        # Grid yerleşimi
        self.tree.grid(row=0, column=0, sticky="nsew")
        scrollbar_y.grid(row=0, column=1, sticky="ns")
        scrollbar_x.grid(row=1, column=0, sticky="ew")
        
        tree_frame.grid_rowconfigure(0, weight=1)
        tree_frame.grid_columnconfigure(0, weight=1)
        
        # Geciken satırları kırmızı yap
        self.tree.tag_configure("geciken", background="#dc2626", foreground="white")
    
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
            else:
                self.combo_kitap.set("")
        except Exception as e:
            messagebox.showerror("Hata", f"Kitaplar yüklenemedi: {str(e)}")
    
    def load_uyeler_combo(self):
        """Üyeleri combo'ya yükle"""
        try:
            cursor = self.db.get_connection().cursor()
            # Kullanicilar tablosundan üyeleri çek
            cursor.execute("SELECT KullaniciID, AdSoyad FROM Kullanicilar WHERE AktifMi = 1 AND Rol = 'Uye'")
            rows = cursor.fetchall()
            
            uyeler = [f"{row.KullaniciID} - {row.AdSoyad}" for row in rows]
            self.combo_uye.configure(values=uyeler)
            if uyeler:
                self.combo_uye.set(uyeler[0])
            else:
                self.combo_uye.set("")
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
                JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
                WHERE o.Durum = 'Odunc'
            """)
            rows = cursor.fetchall()
            
            islemler = [f"{row.IslemID} - {row.Baslik} - {row.AdSoyad}" for row in rows]
            self.combo_islem.configure(values=islemler)
            if islemler:
                self.combo_islem.set(islemler[0])
            else:
                self.combo_islem.set("")
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
                JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
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
            
        except Exception as e:
            messagebox.showerror("Hata", f"İşlemler yüklenemedi: {str(e)}")
    
    def add_odunc(self):
        """Yeni ödünç işlemi"""
        try:
            kitap_text = self.combo_kitap.get()
            if not kitap_text:
                messagebox.showwarning("Uyarı", "Lütfen bir kitap seçin!")
                return
            
            uye_text = self.combo_uye.get()
            if not uye_text:
                messagebox.showwarning("Uyarı", "Lütfen bir üye seçin!")
                return
            
            # Kitap ID'sini al
            kitap_id = int(kitap_text.split(" - ")[0])
            
            # Üye ID'sini al
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
            islem_text = self.combo_islem.get()
            if not islem_text:
                messagebox.showwarning("Uyarı", "Lütfen bir ödünç işlemi seçin!")
                return
            
            # İşlem ID'sini al
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