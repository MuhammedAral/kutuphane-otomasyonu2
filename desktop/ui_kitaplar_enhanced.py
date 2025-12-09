import customtkinter as ctk
from tkinter import ttk, messagebox, filedialog
import tkinter as tk
import pandas as pd
import cv2
from pyzbar.pyzbar import decode
import threading
from PIL import Image, ImageTk

class KitaplarEnhancedFrame(ctk.CTkFrame):
    def __init__(self, parent, db):
        super().__init__(parent)
        self.db = db
        self.selected_id = None
        self.barcode_window = None
        
        # Dark tema için Treeview stili
        self.setup_treeview_style()
        
        # Başlık ve aksiyonlar
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=20)
        
        title = ctk.CTkLabel(
            header,
            text="📚 Kitap Yönetimi",
            font=ctk.CTkFont(size=24, weight="bold")
        )
        title.pack(side="left")
        
        # Sağ taraf butonlar
        action_frame = ctk.CTkFrame(header, fg_color="transparent")
        action_frame.pack(side="right")
        
        ctk.CTkButton(
            action_frame,
            text="📷 Barkod Tara",
            command=self.scan_barcode,
            width=140,
            height=35,
            fg_color="#8b5cf6"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            action_frame,
            text="📥 Excel'den Al",
            command=self.import_from_excel,
            width=140,
            height=35,
            fg_color="#10b981"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            action_frame,
            text="📤 Excel'e Aktar",
            command=self.export_to_excel,
            width=140,
            height=35,
            fg_color="#f59e0b"
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
        self.load_kitaplar()
    
    def create_form(self, parent):
        """Form alanını oluştur"""
        # Form başlığı
        header = ctk.CTkFrame(parent, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=15)
        
        ctk.CTkLabel(
            header,
            text="Kitap Bilgileri",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(side="left")
        
        # İki sütunlu form
        form_grid = ctk.CTkFrame(parent, fg_color="transparent")
        form_grid.pack(fill="x", padx=20, pady=(0, 15))
        
        # Sol sütun
        left_col = ctk.CTkFrame(form_grid, fg_color="transparent")
        left_col.pack(side="left", fill="both", expand=True, padx=(0, 10))
        
        # Başlık
        ctk.CTkLabel(left_col, text="Başlık *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_baslik = ctk.CTkEntry(left_col, height=35, placeholder_text="Kitap başlığı")
        self.entry_baslik.pack(fill="x", pady=(0, 10))
        
        # Yazar
        ctk.CTkLabel(left_col, text="Yazar *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_yazar = ctk.CTkEntry(left_col, height=35, placeholder_text="Yazar adı")
        self.entry_yazar.pack(fill="x", pady=(0, 10))
        
        # ISBN
        ctk.CTkLabel(left_col, text="ISBN", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_isbn = ctk.CTkEntry(left_col, height=35, placeholder_text="ISBN numarası")
        self.entry_isbn.pack(fill="x", pady=(0, 10))
        
        # Barkod
        barcode_frame = ctk.CTkFrame(left_col, fg_color="transparent")
        barcode_frame.pack(fill="x", pady=(5, 10))
        
        barcode_left = ctk.CTkFrame(barcode_frame, fg_color="transparent")
        barcode_left.pack(side="left", fill="both", expand=True)
        
        ctk.CTkLabel(barcode_left, text="Barkod", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(0, 2))
        self.entry_barkod = ctk.CTkEntry(barcode_left, height=35, placeholder_text="Barkod numarası")
        self.entry_barkod.pack(fill="x")
        
        ctk.CTkButton(
            barcode_frame,
            text="📷",
            command=self.scan_barcode,
            width=40,
            height=35
        ).pack(side="right", padx=(5, 0), pady=(18, 0))
        
        # Sağ sütun
        right_col = ctk.CTkFrame(form_grid, fg_color="transparent")
        right_col.pack(side="right", fill="both", expand=True, padx=(10, 0))
        
        # Yayın Yılı
        ctk.CTkLabel(right_col, text="Yayın Yılı", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_yayin = ctk.CTkEntry(right_col, height=35, placeholder_text="2024")
        self.entry_yayin.pack(fill="x", pady=(0, 10))
        
        # Kitap Türü
        ctk.CTkLabel(right_col, text="Kitap Türü *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.combo_tur = ctk.CTkComboBox(right_col, height=35, state="readonly")
        self.combo_tur.pack(fill="x", pady=(0, 10))
        self.load_book_types()
        
        # Stok Adedi
        ctk.CTkLabel(right_col, text="Stok Adedi *", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_stok = ctk.CTkEntry(right_col, height=35, placeholder_text="0")
        self.entry_stok.pack(fill="x", pady=(0, 10))
        
        # Raf No
        ctk.CTkLabel(right_col, text="Raf No", font=ctk.CTkFont(size=11, weight="bold")).pack(anchor="w", pady=(5, 2))
        self.entry_raf = ctk.CTkEntry(right_col, height=35, placeholder_text="A1-R2")
        self.entry_raf.pack(fill="x", pady=(0, 10))
        
        # Butonlar
        btn_frame = ctk.CTkFrame(parent, fg_color="transparent")
        btn_frame.pack(fill="x", padx=20, pady=(10, 15))
        
        ctk.CTkButton(
            btn_frame,
            text="➕ Ekle",
            command=self.add_kitap,
            height=40,
            width=120,
            fg_color="#10b981"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            btn_frame,
            text="🔄 Güncelle",
            command=self.update_kitap,
            height=40,
            width=120,
            fg_color="#3b82f6"
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            btn_frame,
            text="🗑️ Sil",
            command=self.delete_kitap,
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
            placeholder_text="Kitap adı, yazar veya barkod ile ara...",
            height=35
        )
        self.entry_search.pack(side="left", fill="x", expand=True, padx=5)
        
        ctk.CTkButton(
            search_frame,
            text="Ara",
            command=self.search_kitap,
            width=100,
            height=35
        ).pack(side="left", padx=5)
        
        ctk.CTkButton(
            search_frame,
            text="Tümünü Göster",
            command=self.load_kitaplar,
            width=120,
            height=35,
            fg_color="gray"
        ).pack(side="left")
        
        # Treeview
        tree_frame = ctk.CTkFrame(parent, fg_color="#2b2b2b")
        tree_frame.pack(fill="both", expand=True, padx=15, pady=(0, 15))
        
        columns = ("ID", "Başlık", "Yazar", "ISBN", "Barkod", "Tür", "Yayın", "Stok", "Mevcut", "Raf")
        self.tree = ttk.Treeview(tree_frame, columns=columns, show="headings", height=12, style="Dark.Treeview")
        
        # Sütun başlıkları ve genişlikler
        column_widths = {
            "ID": 50,
            "Başlık": 200,
            "Yazar": 150,
            "ISBN": 100,
            "Barkod": 100,
            "Tür": 100,
            "Yayın": 70,
            "Stok": 60,
            "Mevcut": 70,
            "Raf": 70
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
        self.entry_search.bind("<Return>", lambda e: self.search_kitap())
    
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
    
    def load_book_types(self):
        """Kitap türlerini yükle"""
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("SELECT TurAdi FROM KitapTurleri ORDER BY TurAdi")
            rows = cursor.fetchall()
            
            types = [row.TurAdi for row in rows]
            self.combo_tur.configure(values=types)
            if types:
                self.combo_tur.set(types[0])
        except Exception as e:
            print(f"Türler yüklenemedi: {e}")
    
    def load_kitaplar(self):
        """Kitapları yükle"""
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT k.*, kt.TurAdi
                FROM Kitaplar k
                LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                ORDER BY k.KitapID DESC
            """)
            rows = cursor.fetchall()
            
            for row in rows:
                self.tree.insert("", "end", values=(
                    row.KitapID,
                    row.Baslik,
                    row.Yazar,
                    row.ISBN or "",
                    row.Barkod or "",
                    row.TurAdi or "",
                    row.YayinYili or "",
                    row.StokAdedi,
                    row.MevcutAdet,
                    row.RafNo or ""
                ))
        except Exception as e:
            messagebox.showerror("Hata", f"Kitaplar yüklenemedi: {str(e)}")
    
    def add_kitap(self):
        """Yeni kitap ekle"""
        baslik = self.entry_baslik.get().strip()
        yazar = self.entry_yazar.get().strip()
        tur = self.combo_tur.get()
        
        if not baslik or not yazar or not tur:
            messagebox.showwarning("Uyarı", "Başlık, Yazar ve Tür alanları zorunludur!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            
            # Tür ID'sini al
            cursor.execute("SELECT TurID FROM KitapTurleri WHERE TurAdi = ?", tur)
            tur_row = cursor.fetchone()
            if not tur_row:
                messagebox.showerror("Hata", "Geçersiz kitap türü!")
                return
            
            stok = int(self.entry_stok.get() or 0)
            
            cursor.execute("""
                INSERT INTO Kitaplar (Baslik, Yazar, ISBN, Barkod, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, baslik, yazar, 
                 self.entry_isbn.get() or None,
                 self.entry_barkod.get() or None,
                 int(self.entry_yayin.get()) if self.entry_yayin.get() else None,
                 tur_row.TurID, stok, stok,
                 self.entry_raf.get() or None)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Kitap başarıyla eklendi!")
            self.clear_form()
            self.load_kitaplar()
        except Exception as e:
            messagebox.showerror("Hata", f"Kitap eklenemedi: {str(e)}")
    
    def update_kitap(self):
        """Kitap güncelle"""
        if not self.selected_id:
            messagebox.showwarning("Uyarı", "Lütfen güncellenecek kitabı seçin!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            tur = self.combo_tur.get()
            
            cursor.execute("SELECT TurID FROM KitapTurleri WHERE TurAdi = ?", tur)
            tur_row = cursor.fetchone()
            
            cursor.execute("""
                UPDATE Kitaplar 
                SET Baslik = ?, Yazar = ?, ISBN = ?, Barkod = ?, YayinYili = ?, TurID = ?, StokAdedi = ?, RafNo = ?
                WHERE KitapID = ?
            """, self.entry_baslik.get(), self.entry_yazar.get(),
                 self.entry_isbn.get() or None,
                 self.entry_barkod.get() or None,
                 int(self.entry_yayin.get()) if self.entry_yayin.get() else None,
                 tur_row.TurID if tur_row else None,
                 int(self.entry_stok.get() or 0),
                 self.entry_raf.get() or None,
                 self.selected_id)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Kitap başarıyla güncellendi!")
            self.clear_form()
            self.load_kitaplar()
        except Exception as e:
            messagebox.showerror("Hata", f"Kitap güncellenemedi: {str(e)}")
    
    def delete_kitap(self):
        """Kitap sil"""
        if not self.selected_id:
            messagebox.showwarning("Uyarı", "Lütfen silinecek kitabı seçin!")
            return
        
        if messagebox.askyesno("Onay", "Bu kitabı silmek istediğinizden emin misiniz?"):
            try:
                cursor = self.db.get_connection().cursor()
                cursor.execute("DELETE FROM Kitaplar WHERE KitapID = ?", self.selected_id)
                self.db.get_connection().commit()
                messagebox.showinfo("Başarılı", "Kitap başarıyla silindi!")
                self.clear_form()
                self.load_kitaplar()
            except Exception as e:
                messagebox.showerror("Hata", f"Kitap silinemedi: {str(e)}")
    
    def search_kitap(self):
        """Kitap ara"""
        search_text = self.entry_search.get().strip()
        
        for item in self.tree.get_children():
            self.tree.delete(item)
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT k.*, kt.TurAdi
                FROM Kitaplar k
                LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                WHERE k.Baslik LIKE ? OR k.Yazar LIKE ? OR k.Barkod LIKE ?
                ORDER BY k.KitapID DESC
            """, f"%{search_text}%", f"%{search_text}%", f"%{search_text}%")
            rows = cursor.fetchall()
            
            for row in rows:
                self.tree.insert("", "end", values=(
                    row.KitapID, row.Baslik, row.Yazar, row.ISBN or "",
                    row.Barkod or "", row.TurAdi or "", row.YayinYili or "",
                    row.StokAdedi, row.MevcutAdet, row.RafNo or ""
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
            self.entry_baslik.delete(0, 'end')
            self.entry_baslik.insert(0, values[1])
            self.entry_yazar.delete(0, 'end')
            self.entry_yazar.insert(0, values[2])
            self.entry_isbn.delete(0, 'end')
            self.entry_isbn.insert(0, values[3])
            self.entry_barkod.delete(0, 'end')
            self.entry_barkod.insert(0, values[4])
            if values[5]:
                self.combo_tur.set(values[5])
            self.entry_yayin.delete(0, 'end')
            self.entry_yayin.insert(0, values[6])
            self.entry_stok.delete(0, 'end')
            self.entry_stok.insert(0, values[7])
            self.entry_raf.delete(0, 'end')
            self.entry_raf.insert(0, values[9])
    
    def clear_form(self):
        """Formu temizle"""
        self.selected_id = None
        self.entry_baslik.delete(0, 'end')
        self.entry_yazar.delete(0, 'end')
        self.entry_isbn.delete(0, 'end')
        self.entry_barkod.delete(0, 'end')
        self.entry_yayin.delete(0, 'end')
        self.entry_stok.delete(0, 'end')
        self.entry_raf.delete(0, 'end')
    
    def import_from_excel(self):
        """Excel'den kitap al"""
        file_path = filedialog.askopenfilename(
            title="Excel Dosyası Seç",
            filetypes=[("Excel files", "*.xlsx *.xls")]
        )
        
        if not file_path:
            return
        
        try:
            df = pd.read_excel(file_path)
            
            # Gerekli sütunları kontrol et
            required_columns = ['Baslik', 'Yazar']
            if not all(col in df.columns for col in required_columns):
                messagebox.showerror("Hata", "Excel dosyası 'Baslik' ve 'Yazar' sütunlarını içermelidir!")
                return
            
            cursor = self.db.get_connection().cursor()
            added = 0
            errors = 0
            
            for _, row in df.iterrows():
                try:
                    # Tür ID'sini al (varsa)
                    tur_id = None
                    if 'Tur' in df.columns and pd.notna(row['Tur']):
                        cursor.execute("SELECT TurID FROM KitapTurleri WHERE TurAdi = ?", row['Tur'])
                        tur_row = cursor.fetchone()
                        if tur_row:
                            tur_id = tur_row.TurID
                    
                    stok = int(row.get('Stok', 1))
                    
                    cursor.execute("""
                        INSERT INTO Kitaplar (Baslik, Yazar, ISBN, Barkod, YayinYili, TurID, StokAdedi, MevcutAdet, RafNo)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """, str(row['Baslik']), str(row['Yazar']),
                         str(row.get('ISBN', '')) if 'ISBN' in df.columns and pd.notna(row.get('ISBN')) else None,
                         str(row.get('Barkod', '')) if 'Barkod' in df.columns and pd.notna(row.get('Barkod')) else None,
                         int(row.get('YayinYili')) if 'YayinYili' in df.columns and pd.notna(row.get('YayinYili')) else None,
                         tur_id, stok, stok,
                         str(row.get('RafNo', '')) if 'RafNo' in df.columns and pd.notna(row.get('RafNo')) else None)
                    added += 1
                except Exception as e:
                    errors += 1
                    print(f"Satır hatası: {e}")
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", f"{added} kitap eklendi!\n{errors} hata oluştu.")
            self.load_kitaplar()
            
        except Exception as e:
            messagebox.showerror("Hata", f"Excel dosyası okunamadı: {str(e)}")
    
    def export_to_excel(self):
        """Kitapları Excel'e aktar"""
        file_path = filedialog.asksaveasfilename(
            title="Excel Dosyası Kaydet",
            defaultextension=".xlsx",
            filetypes=[("Excel files", "*.xlsx")]
        )
        
        if not file_path:
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT 
                    k.Baslik, k.Yazar, k.ISBN, k.Barkod, 
                    k.YayinYili, kt.TurAdi as Tur, 
                    k.StokAdedi as Stok, k.MevcutAdet as Mevcut, k.RafNo
                FROM Kitaplar k
                LEFT JOIN KitapTurleri kt ON k.TurID = kt.TurID
                ORDER BY k.KitapID DESC
            """)
            
            rows = cursor.fetchall()
            columns = [desc[0] for desc in cursor.description]
            
            df = pd.DataFrame.from_records(rows, columns=columns)
            df.to_excel(file_path, index=False)
            
            messagebox.showinfo("Başarılı", f"Kitaplar Excel'e aktarıldı!\n{len(rows)} kayıt")
            
        except Exception as e:
            messagebox.showerror("Hata", f"Excel dosyası oluşturulamadı: {str(e)}")
    
    def scan_barcode(self):
        """Barkod tarama penceresi aç"""
        if self.barcode_window and self.barcode_window.winfo_exists():
            self.barcode_window.lift()
            return
        
        self.barcode_window = BarcodeScanner(self, self.on_barcode_scanned)
    
    def on_barcode_scanned(self, barcode):
        """Barkod tarandığında"""
        self.entry_barkod.delete(0, 'end')
        self.entry_barkod.insert(0, barcode)
        messagebox.showinfo("Başarılı", f"Barkod tarandı: {barcode}")


class BarcodeScanner(ctk.CTkToplevel):
    def __init__(self, parent, callback):
        super().__init__(parent)
        
        self.callback = callback
        self.running = False
        
        self.title("📷 Barkod Tarayıcı")
        self.geometry("640x520")
        self.resizable(False, False)
        
        # Başlık
        title = ctk.CTkLabel(
            self,
            text="📷 Barkod Tarayıcı",
            font=ctk.CTkFont(size=20, weight="bold")
        )
        title.pack(pady=15)
        
        # Kamera görüntüsü
        self.camera_label = ctk.CTkLabel(self, text="")
        self.camera_label.pack(pady=10)
        
        # Bilgi
        self.info_label = ctk.CTkLabel(
            self,
            text="Barkodu kamera önüne tutun...",
            font=ctk.CTkFont(size=12)
        )
        self.info_label.pack(pady=5)
        
        # Kapat butonu
        ctk.CTkButton(
            self,
            text="❌ Kapat",
            command=self.stop_camera,
            height=40,
            width=120,
            fg_color="red"
        ).pack(pady=10)
        
        # Kamerayı başlat
        self.start_camera()
        
        self.protocol("WM_DELETE_WINDOW", self.stop_camera)
    
    def start_camera(self):
        """Kamera başlat"""
        self.running = True
        self.cap = cv2.VideoCapture(0)
        self.update_camera()
    
    def update_camera(self):
        """Kamera görüntüsünü güncelle"""
        if not self.running:
            return
        
        ret, frame = self.cap.read()
        if ret:
            # Barkod tara
            barcodes = decode(frame)
            
            for barcode in barcodes:
                # Barkod bulundu
                barcode_data = barcode.data.decode('utf-8')
                
                # Görüntüde işaretle
                points = barcode.polygon
                if points:
                    pts = [(point.x, point.y) for point in points]
                    import numpy as np
                    cv2.polylines(frame, [np.array(pts, dtype=np.int32)], True, (0, 255, 0), 2)
                
                # Callback'i çağır ve pencereyi kapat
                self.stop_camera()
                self.callback(barcode_data)
                return
            
            # Görüntüyü göster
            frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            frame = cv2.resize(frame, (600, 400))
            img = Image.fromarray(frame)
            imgtk = ImageTk.PhotoImage(image=img)
            self.camera_label.imgtk = imgtk
            self.camera_label.configure(image=imgtk)
        
        # Tekrar güncelle
        if self.running:
            self.after(30, self.update_camera)
    
    def stop_camera(self):
        """Kamerayı durdur ve pencereyi kapat"""
        self.running = False
        if hasattr(self, 'cap') and self.cap:
            self.cap.release()
        self.destroy()