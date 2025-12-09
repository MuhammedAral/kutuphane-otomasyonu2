import customtkinter as ctk
from tkinter import ttk, messagebox

class UyelerFrame(ctk.CTkFrame):
    def __init__(self, parent, db):
        super().__init__(parent)
        self.db = db
        self.selected_id = None
        
        # Dark tema için Treeview stili
        self.setup_treeview_style()
        
        # Başlık ve aksiyonlar
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=20)
        
        title = ctk.CTkLabel(
            header,
            text="👥 Üye Yönetimi",
            font=ctk.CTkFont(size=24, weight="bold")
        )
        title.pack(side="left")
        
        # Sağ taraf butonlar
        action_frame = ctk.CTkFrame(header, fg_color="transparent")
        action_frame.pack(side="right")
        
        ctk.CTkButton(
            action_frame,
            text="🔄 Yenile",
            command=self.load_uyeler,
            width=100,
            height=35
        ).pack(side="left", padx=5)
        
        # Form ve tablo için container
        content = ctk.CTkFrame(self, fg_color="transparent")
        content.pack(fill="both", expand=True, padx=20, pady=(0, 20))
        
        # Form alanı (üstte)
        form_frame = ctk.CTkFrame(content)
        form_frame.pack(fill="x", pady=(0, 15))
        
        self.create_form(form_frame)
        
        # Tablo alanı (altta)
        table_frame = ctk.CTkFrame(content)
        table_frame.pack(fill="both", expand=True)
        
        self.create_table(table_frame)
        
        # Verileri yükle
        self.load_uyeler()
    
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
    
    def create_form(self, parent):
        """Form alanını oluştur"""
        # Form başlığı
        header = ctk.CTkFrame(parent, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=15)
        
        ctk.CTkLabel(
            header,
            text="Üye Bilgileri",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(side="left")
        
        # İki sütunlu form
        form_grid = ctk.CTkFrame(parent, fg_color="transparent")
        form_grid.pack(fill="x", padx=20, pady=(0, 15))
        
        # Sol sütun
        left_col = ctk.CTkFrame(form_grid, fg_color="transparent")
        left_col.pack(side="left", fill="both", expand=True, padx=(0, 10))
        
        # Ad Soyad
        ctk.CTkLabel(left_col, text="Ad Soyad *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_adsoyad = ctk.CTkEntry(left_col, height=35, placeholder_text="Üye adı soyadı")
        self.entry_adsoyad.pack(fill="x", pady=(0, 10))
        
        # Email
        ctk.CTkLabel(left_col, text="Email", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_email = ctk.CTkEntry(left_col, height=35, placeholder_text="ornek@email.com")
        self.entry_email.pack(fill="x", pady=(0, 10))
        
        # Sağ sütun
        right_col = ctk.CTkFrame(form_grid, fg_color="transparent")
        right_col.pack(side="right", fill="both", expand=True, padx=(10, 0))
        
        # Telefon
        ctk.CTkLabel(right_col, text="Telefon", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_telefon = ctk.CTkEntry(right_col, height=35, placeholder_text="05XX XXX XX XX")
        self.entry_telefon.pack(fill="x", pady=(0, 10))
        
        # Adres
        ctk.CTkLabel(right_col, text="Adres", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_adres = ctk.CTkEntry(right_col, height=35, placeholder_text="Adres bilgisi")
        self.entry_adres.pack(fill="x", pady=(0, 10))
        
        # Butonlar
        btn_frame = ctk.CTkFrame(parent, fg_color="transparent")
        btn_frame.pack(fill="x", padx=20, pady=(10, 15))
        
        ctk.CTkButton(
            btn_frame,
            text="➕ Ekle",
            command=self.add_uye,
            height=40,
            width=120,
            fg_color="#10b981"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            btn_frame,
            text="🔄 Güncelle",
            command=self.update_uye,
            height=40,
            width=120,
            fg_color="#3b82f6"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            btn_frame,
            text="🗑️ Sil",
            command=self.delete_uye,
            height=40,
            width=120,
            fg_color="#ef4444"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            btn_frame,
            text="🧹 Temizle",
            command=self.clear_form,
            height=40,
            width=120,
            fg_color="gray"
        ).pack(side="left", padx=5)
    
    def create_table(self, parent):
        """Tablo alanını oluştur"""
        # Arama
        search_frame = ctk.CTkFrame(parent, fg_color="transparent")
        search_frame.pack(fill="x", padx=15, pady=15)
        
        ctk.CTkLabel(
            search_frame,
            text="🔍",
            font=ctk.CTkFont(size=18)
        ).pack(side="left", padx=(0, 5))
        
        self.entry_search = ctk.CTkEntry(
            search_frame,
            placeholder_text="Üye adı, email veya telefon ile ara...",
            height=35
        )
        self.entry_search.pack(side="left", fill="x", expand=True, padx=5)
        self.entry_search.bind("<Return>", lambda e: self.search_uye())
        
        ctk.CTkButton(
            search_frame,
            text="Ara",
            command=self.search_uye,
            width=100,
            height=35
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            search_frame,
            text="Tümünü Göster",
            command=self.load_uyeler,
            width=120,
            height=35,
            fg_color="gray"
        ).pack(side="left")
        
        # Treeview
        tree_frame = ctk.CTkFrame(parent, fg_color="#2b2b2b")
        tree_frame.pack(fill="both", expand=True, padx=15, pady=(0, 15))
        
        columns = ("ID", "Ad Soyad", "Email", "Telefon", "Kayıt Tarihi", "Aktif")
        self.tree = ttk.Treeview(tree_frame, columns=columns, show="headings", height=12, style="Dark.Treeview")
        
        # Sütun başlıkları ve genişlikler
        column_widths = {
            "ID": 50,
            "Ad Soyad": 180,
            "Email": 200,
            "Telefon": 120,
            "Kayıt Tarihi": 120,
            "Aktif": 60
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
        
        # Tıklama eventi
        self.tree.bind("<ButtonRelease-1>", self.on_tree_select)
    
    def load_uyeler(self):
        """Üyeleri yükle"""
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            # Kullanicilar tablosundan üyeleri çek (Rol = 'Uye' olanlar)
            cursor.execute("""
                SELECT KullaniciID, AdSoyad, Email, Telefon, OlusturmaTarihi, AktifMi 
                FROM Kullanicilar 
                WHERE Rol = 'Uye'
                ORDER BY KullaniciID DESC
            """)
            rows = cursor.fetchall()
            
            for row in rows:
                self.tree.insert("", "end", values=(
                    row.KullaniciID,
                    row.AdSoyad,
                    row.Email or "",
                    row.Telefon or "",
                    row.OlusturmaTarihi.strftime('%Y-%m-%d') if row.OlusturmaTarihi else "",
                    "✓" if row.AktifMi else "✗"
                ))
        except Exception as e:
            messagebox.showerror("Hata", f"Üyeler yüklenemedi: {str(e)}")
    
    def add_uye(self):
        """Yeni üye ekle"""
        adsoyad = self.entry_adsoyad.get().strip()
        
        if not adsoyad:
            messagebox.showwarning("Uyarı", "Ad Soyad alanı zorunludur!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            
            # Kullanıcı adı oluştur (ad soyaddan)
            kullanici_adi = adsoyad.lower().replace(" ", "").replace("ı", "i").replace("ğ", "g").replace("ü", "u").replace("ş", "s").replace("ö", "o").replace("ç", "c")
            
            # Aynı kullanıcı adı var mı kontrol et
            cursor.execute("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = ?", kullanici_adi)
            if cursor.fetchone()[0] > 0:
                kullanici_adi = kullanici_adi + str(int(__import__('time').time()) % 1000)
            
            # Varsayılan şifre (hashlenmiş)
            import hashlib
            default_password = hashlib.sha256("123456".encode()).hexdigest()
            
            cursor.execute("""
                INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol)
                VALUES (?, ?, ?, ?, ?, 'Uye')
            """, kullanici_adi, default_password, adsoyad, 
                 self.entry_email.get() or None,
                 self.entry_telefon.get() or None)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", f"Üye başarıyla eklendi!\n\nKullanıcı adı: {kullanici_adi}\nVarsayılan şifre: 123456")
            self.clear_form()
            self.load_uyeler()
        except Exception as e:
            messagebox.showerror("Hata", f"Üye eklenemedi: {str(e)}")
    
    def update_uye(self):
        """Üye güncelle"""
        if not self.selected_id:
            messagebox.showwarning("Uyarı", "Lütfen güncellenecek üyeyi seçin!")
            return
        
        adsoyad = self.entry_adsoyad.get().strip()
        if not adsoyad:
            messagebox.showwarning("Uyarı", "Ad Soyad alanı zorunludur!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                UPDATE Kullanicilar 
                SET AdSoyad = ?, Email = ?, Telefon = ?
                WHERE KullaniciID = ?
            """, adsoyad, self.entry_email.get() or None,
                 self.entry_telefon.get() or None, self.selected_id)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Üye başarıyla güncellendi!")
            self.clear_form()
            self.load_uyeler()
        except Exception as e:
            messagebox.showerror("Hata", f"Üye güncellenemedi: {str(e)}")
    
    def delete_uye(self):
        """Üye sil"""
        if not self.selected_id:
            messagebox.showwarning("Uyarı", "Lütfen silinecek üyeyi seçin!")
            return
        
        if messagebox.askyesno("Onay", "Bu üyeyi silmek istediğinizden emin misiniz?"):
            try:
                cursor = self.db.get_connection().cursor()
                # Üyeyi pasif yap (soft delete)
                cursor.execute("UPDATE Kullanicilar SET AktifMi = 0 WHERE KullaniciID = ?", self.selected_id)
                self.db.get_connection().commit()
                messagebox.showinfo("Başarılı", "Üye başarıyla silindi!")
                self.clear_form()
                self.load_uyeler()
            except Exception as e:
                messagebox.showerror("Hata", f"Üye silinemedi: {str(e)}")
    
    def search_uye(self):
        """Üye ara"""
        search_text = self.entry_search.get().strip()
        
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT KullaniciID, AdSoyad, Email, Telefon, OlusturmaTarihi, AktifMi 
                FROM Kullanicilar 
                WHERE Rol = 'Uye' AND (AdSoyad LIKE ? OR Email LIKE ? OR Telefon LIKE ?)
                ORDER BY KullaniciID DESC
            """, f"%{search_text}%", f"%{search_text}%", f"%{search_text}%")
            rows = cursor.fetchall()
            
            for row in rows:
                self.tree.insert("", "end", values=(
                    row.KullaniciID, row.AdSoyad, row.Email or "", row.Telefon or "",
                    row.OlusturmaTarihi.strftime('%Y-%m-%d') if row.OlusturmaTarihi else "",
                    "✓" if row.AktifMi else "✗"
                ))
        except Exception as e:
            messagebox.showerror("Hata", f"Arama yapılamadı: {str(e)}")
    
    def on_tree_select(self, event):
        """Tablodan seçim yapıldığında"""
        selected = self.tree.selection()
        if selected:
            item = self.tree.item(selected[0])
            values = item['values']
            
            self.selected_id = values[0]
            self.entry_adsoyad.delete(0, 'end')
            self.entry_adsoyad.insert(0, values[1])
            self.entry_email.delete(0, 'end')
            self.entry_email.insert(0, values[2])
            self.entry_telefon.delete(0, 'end')
            self.entry_telefon.insert(0, values[3])
    
    def clear_form(self):
        """Formu temizle"""
        self.selected_id = None
        self.entry_adsoyad.delete(0, 'end')
        self.entry_email.delete(0, 'end')
        self.entry_telefon.delete(0, 'end')
        self.entry_adres.delete(0, 'end')