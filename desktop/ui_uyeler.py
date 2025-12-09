import customtkinter as ctk
from tkinter import ttk, messagebox

class UyelerFrame(ctk.CTkFrame):
    def __init__(self, parent, db):
        super().__init__(parent)
        self.db = db
        
        # Başlık
        title = ctk.CTkLabel(self, text="👥 Üye Yönetimi", font=ctk.CTkFont(size=24, weight="bold"))
        title.pack(pady=20)
        
        # Form
        form_frame = ctk.CTkFrame(self)
        form_frame.pack(fill="x", padx=20, pady=10)
        
        form_title = ctk.CTkLabel(form_frame, text="Yeni Üye Ekle", font=ctk.CTkFont(size=16, weight="bold"))
        form_title.grid(row=0, column=0, columnspan=4, pady=10)
        
        ctk.CTkLabel(form_frame, text="Ad Soyad:").grid(row=1, column=0, padx=10, pady=5, sticky="e")
        self.entry_adsoyad = ctk.CTkEntry(form_frame, width=200)
        self.entry_adsoyad.grid(row=1, column=1, padx=10, pady=5)
        
        ctk.CTkLabel(form_frame, text="Email:").grid(row=1, column=2, padx=10, pady=5, sticky="e")
        self.entry_email = ctk.CTkEntry(form_frame, width=200)
        self.entry_email.grid(row=1, column=3, padx=10, pady=5)
        
        ctk.CTkLabel(form_frame, text="Telefon:").grid(row=2, column=0, padx=10, pady=5, sticky="e")
        self.entry_telefon = ctk.CTkEntry(form_frame, width=200)
        self.entry_telefon.grid(row=2, column=1, padx=10, pady=5)
        
        ctk.CTkLabel(form_frame, text="Adres:").grid(row=2, column=2, padx=10, pady=5, sticky="e")
        self.entry_adres = ctk.CTkEntry(form_frame, width=200)
        self.entry_adres.grid(row=2, column=3, padx=10, pady=5)
        
        # Butonlar
        btn_frame = ctk.CTkFrame(form_frame, fg_color="transparent")
        btn_frame.grid(row=3, column=0, columnspan=4, pady=15)
        
        ctk.CTkButton(btn_frame, text="➕ Ekle", command=self.add_uye, width=120).pack(side="left", padx=5)
        ctk.CTkButton(btn_frame, text="🔄 Güncelle", command=self.update_uye, width=120).pack(side="left", padx=5)
        ctk.CTkButton(btn_frame, text="🗑️ Sil", command=self.delete_uye, fg_color="red", width=120).pack(side="left", padx=5)
        ctk.CTkButton(btn_frame, text="🧹 Temizle", command=self.clear_form, fg_color="gray", width=120).pack(side="left", padx=5)
        
        # Tablo
        table_frame = ctk.CTkFrame(self)
        table_frame.pack(fill="both", expand=True, padx=20, pady=10)
        
        # Arama
        search_frame = ctk.CTkFrame(table_frame, fg_color="transparent")
        search_frame.pack(fill="x", pady=10)
        
        ctk.CTkLabel(search_frame, text="🔍 Ara:", font=ctk.CTkFont(size=14)).pack(side="left", padx=10)
        self.entry_search = ctk.CTkEntry(search_frame, width=300, placeholder_text="Üye adı...")
        self.entry_search.pack(side="left", padx=5)
        ctk.CTkButton(search_frame, text="Ara", command=self.search_uye, width=100).pack(side="left", padx=5)
        ctk.CTkButton(search_frame, text="Tümünü Göster", command=self.load_uyeler, width=120).pack(side="left", padx=5)
        
        # Treeview
        columns = ("ID", "Ad Soyad", "Email", "Telefon", "Adres", "Kayıt Tarihi", "Aktif")
        self.tree = ttk.Treeview(table_frame, columns=columns, show="headings", height=15)
        
        for col in columns:
            self.tree.heading(col, text=col)
            if col == "ID":
                self.tree.column(col, width=50)
            elif col == "Aktif":
                self.tree.column(col, width=60)
            else:
                self.tree.column(col, width=150)
        
        scrollbar = ttk.Scrollbar(table_frame, orient="vertical", command=self.tree.yview)
        self.tree.configure(yscrollcommand=scrollbar.set)
        
        self.tree.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")
        
        self.tree.bind("<ButtonRelease-1>", self.on_tree_select)
        
        self.load_uyeler()
        self.selected_id = None
    
    def load_uyeler(self):
        """Üyeleri yükle"""
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("SELECT * FROM Uyeler ORDER BY UyeID DESC")
            rows = cursor.fetchall()
            
            for row in rows:
                self.tree.insert("", "end", values=(
                    row.UyeID,
                    row.AdSoyad,
                    row.Email or "",
                    row.Telefon or "",
                    row.Adres or "",
                    row.KayitTarihi.strftime('%Y-%m-%d'),
                    "✓" if row.AktifMi else "✗"
                ))
        except Exception as e:
            messagebox.showerror("Hata", f"Üyeler yüklenemedi: {str(e)}")
    
    def add_uye(self):
        """Yeni üye ekle"""
        adsoyad = self.entry_adsoyad.get()
        
        if not adsoyad:
            messagebox.showwarning("Uyarı", "Ad Soyad alanı zorunludur!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                INSERT INTO Uyeler (AdSoyad, Email, Telefon, Adres)
                VALUES (?, ?, ?, ?)
            """, adsoyad, self.entry_email.get() or None,
                 self.entry_telefon.get() or None, self.entry_adres.get() or None)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Üye başarıyla eklendi!")
            self.clear_form()
            self.load_uyeler()
        except Exception as e:
            messagebox.showerror("Hata", f"Üye eklenemedi: {str(e)}")
    
    def update_uye(self):
        """Üye güncelle"""
        if not self.selected_id:
            messagebox.showwarning("Uyarı", "Lütfen güncellenecek üyeyi seçin!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                UPDATE Uyeler 
                SET AdSoyad = ?, Email = ?, Telefon = ?, Adres = ?
                WHERE UyeID = ?
            """, self.entry_adsoyad.get(), self.entry_email.get() or None,
                 self.entry_telefon.get() or None, self.entry_adres.get() or None, self.selected_id)
            
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
                cursor.execute("DELETE FROM Uyeler WHERE UyeID = ?", self.selected_id)
                self.db.get_connection().commit()
                messagebox.showinfo("Başarılı", "Üye başarıyla silindi!")
                self.clear_form()
                self.load_uyeler()
            except Exception as e:
                messagebox.showerror("Hata", f"Üye silinemedi: {str(e)}")
    
    def search_uye(self):
        """Üye ara"""
        search_text = self.entry_search.get()
        
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT * FROM Uyeler 
                WHERE AdSoyad LIKE ?
                ORDER BY UyeID DESC
            """, f"%{search_text}%")
            rows = cursor.fetchall()
            
            for row in rows:
                self.tree.insert("", "end", values=(
                    row.UyeID, row.AdSoyad, row.Email or "", row.Telefon or "",
                    row.Adres or "", row.KayitTarihi.strftime('%Y-%m-%d'),
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
            self.entry_adres.delete(0, 'end')
            self.entry_adres.insert(0, values[4])
    
    def clear_form(self):
        """Formu temizle"""
        self.selected_id = None
        self.entry_adsoyad.delete(0, 'end')
        self.entry_email.delete(0, 'end')
        self.entry_telefon.delete(0, 'end')
        self.entry_adres.delete(0, 'end')