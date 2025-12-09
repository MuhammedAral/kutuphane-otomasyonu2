import customtkinter as ctk
from tkinter import messagebox
import re
from datetime import datetime, timedelta

class LoginWindow(ctk.CTk):
    def __init__(self, db):
        super().__init__()
        
        self.db = db
        self.current_user = None
        
        # Pencere ayarları
        self.title("🔐 Kütüphane Giriş")
        self.geometry("500x700")
        self.resizable(False, False)
        
        # Tema
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("blue")
        
        # Ana frame - Gradient efekti için
        self.main_frame = ctk.CTkFrame(self, corner_radius=0)
        self.main_frame.pack(fill="both", expand=True)
        
        # Logo ve başlık
        self.logo_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.logo_frame.pack(pady=40)
        
        self.logo_label = ctk.CTkLabel(
            self.logo_frame,
            text="📚",
            font=ctk.CTkFont(size=80)
        )
        self.logo_label.pack()
        
        self.title_label = ctk.CTkLabel(
            self.logo_frame,
            text="Kütüphane Sistemi",
            font=ctk.CTkFont(size=28, weight="bold")
        )
        self.title_label.pack(pady=10)
        
        self.subtitle_label = ctk.CTkLabel(
            self.logo_frame,
            text="Hesabınıza giriş yapın",
            font=ctk.CTkFont(size=14),
            text_color="gray"
        )
        self.subtitle_label.pack()
        
        # Form frame
        self.form_frame = ctk.CTkFrame(self.main_frame, width=400)
        self.form_frame.pack(pady=20, padx=50, fill="x")
        
        # Kullanıcı adı
        self.username_label = ctk.CTkLabel(
            self.form_frame,
            text="Kullanıcı Adı",
            font=ctk.CTkFont(size=12, weight="bold")
        )
        self.username_label.pack(pady=(20, 5), anchor="w", padx=20)
        
        self.username_entry = ctk.CTkEntry(
            self.form_frame,
            placeholder_text="Kullanıcı adınızı girin",
            height=45,
            font=ctk.CTkFont(size=14)
        )
        self.username_entry.pack(pady=(0, 15), padx=20, fill="x")
        
        # Şifre
        self.password_label = ctk.CTkLabel(
            self.form_frame,
            text="Şifre",
            font=ctk.CTkFont(size=12, weight="bold")
        )
        self.password_label.pack(pady=(0, 5), anchor="w", padx=20)
        
        self.password_entry = ctk.CTkEntry(
            self.form_frame,
            placeholder_text="Şifrenizi girin",
            show="●",
            height=45,
            font=ctk.CTkFont(size=14)
        )
        self.password_entry.pack(pady=(0, 10), padx=20, fill="x")
        
        # Şifremi unuttum
        self.forgot_button = ctk.CTkButton(
            self.form_frame,
            text="Şifremi Unuttum",
            fg_color="transparent",
            text_color=("gray10", "gray90"),
            hover_color=("gray70", "gray30"),
            command=self.show_forgot_password,
            height=25,
            font=ctk.CTkFont(size=11)
        )
        self.forgot_button.pack(pady=(0, 20), anchor="e", padx=20)
        
        # Giriş butonu
        self.login_button = ctk.CTkButton(
            self.form_frame,
            text="🔓 Giriş Yap",
            command=self.login,
            height=50,
            font=ctk.CTkFont(size=16, weight="bold"),
            corner_radius=10
        )
        self.login_button.pack(pady=(0, 20), padx=20, fill="x")
        
        # Kayıt ol
        self.register_frame = ctk.CTkFrame(self.form_frame, fg_color="transparent")
        self.register_frame.pack(pady=(0, 20))
        
        ctk.CTkLabel(
            self.register_frame,
            text="Hesabınız yok mu?",
            font=ctk.CTkFont(size=12)
        ).pack(side="left", padx=5)
        
        self.register_button = ctk.CTkButton(
            self.register_frame,
            text="Kayıt Ol",
            fg_color="transparent",
            text_color=("blue", "lightblue"),
            hover_color=("gray70", "gray30"),
            command=self.show_register,
            width=80,
            height=25,
            font=ctk.CTkFont(size=12, weight="bold")
        )
        self.register_button.pack(side="left")
        
        # Enter tuşu ile giriş
        self.password_entry.bind("<Return>", lambda e: self.login())
        
    def login(self):
        """Giriş işlemi"""
        username = self.username_entry.get().strip()
        password = self.password_entry.get()
        
        if not username or not password:
            messagebox.showwarning("Uyarı", "Lütfen tüm alanları doldurun!")
            return
        
        result = self.db.verify_login(username, password)
        
        if result['success']:
            self.current_user = result
            messagebox.showinfo("Başarılı", f"Hoş geldiniz, {result['ad_soyad']}!")
            self.withdraw()  # Login penceresini gizle
            
            # Rol'e göre ilgili pencereyi aç
            if result['rol'] == 'Yonetici':
                from main_admin import AdminWindow
                admin_window = AdminWindow(self.db, self.current_user)
                admin_window.protocol("WM_DELETE_WINDOW", lambda: self.on_main_close(admin_window))
            else:
                from main_member import MemberWindow
                member_window = MemberWindow(self.db, self.current_user)
                member_window.protocol("WM_DELETE_WINDOW", lambda: self.on_main_close(member_window))
        else:
            messagebox.showerror("Hata", result['message'])
    
    def on_main_close(self, window):
        """Ana pencere kapandığında"""
        window.destroy()
        self.deiconify()  # Login penceresini tekrar göster
        self.username_entry.delete(0, 'end')
        self.password_entry.delete(0, 'end')
    
    def show_register(self):
        """Kayıt ekranını göster"""
        register_window = RegisterWindow(self.db, self)
        register_window.grab_set()
    
    def show_forgot_password(self):
        """Şifremi unuttum ekranını göster"""
        forgot_window = ForgotPasswordWindow(self.db)
        forgot_window.grab_set()


class RegisterWindow(ctk.CTkToplevel):
    def __init__(self, db, parent):
        super().__init__(parent)
        
        self.db = db
        
        # Pencere ayarları
        self.title("📝 Yeni Üye Kaydı")
        self.geometry("500x750")
        self.resizable(False, False)
        
        # Başlık
        title = ctk.CTkLabel(
            self,
            text="📝 Yeni Üye Kaydı",
            font=ctk.CTkFont(size=24, weight="bold")
        )
        title.pack(pady=30)
        
        # Form frame
        form_frame = ctk.CTkFrame(self)
        form_frame.pack(pady=10, padx=40, fill="both", expand=True)
        
        # Form alanları
        fields = [
            ("Ad Soyad", "ad_soyad"),
            ("Kullanıcı Adı", "kullanici_adi"),
            ("Email", "email"),
            ("Telefon", "telefon"),
            ("Şifre", "sifre"),
            ("Şifre Tekrar", "sifre_tekrar")
        ]
        
        self.entries = {}
        
        for label_text, key in fields:
            label = ctk.CTkLabel(
                form_frame,
                text=label_text,
                font=ctk.CTkFont(size=12, weight="bold")
            )
            label.pack(pady=(15, 5), anchor="w", padx=20)
            
            if "sifre" in key.lower():
                entry = ctk.CTkEntry(
                    form_frame,
                    placeholder_text=f"{label_text} girin",
                    show="●",
                    height=40
                )
            else:
                entry = ctk.CTkEntry(
                    form_frame,
                    placeholder_text=f"{label_text} girin",
                    height=40
                )
            entry.pack(padx=20, fill="x")
            self.entries[key] = entry
        
        # Info label
        info_label = ctk.CTkLabel(
            form_frame,
            text="* Email ve telefon şifre sıfırlama için gereklidir",
            font=ctk.CTkFont(size=10),
            text_color="gray"
        )
        info_label.pack(pady=10)
        
        # Butonlar
        btn_frame = ctk.CTkFrame(form_frame, fg_color="transparent")
        btn_frame.pack(pady=20, fill="x", padx=20)
        
        ctk.CTkButton(
            btn_frame,
            text="✅ Kayıt Ol",
            command=self.register,
            height=45,
            font=ctk.CTkFont(size=14, weight="bold")
        ).pack(side="left", expand=True, padx=5, fill="x")
        
        ctk.CTkButton(
            btn_frame,
            text="❌ İptal",
            command=self.destroy,
            height=45,
            fg_color="gray",
            font=ctk.CTkFont(size=14)
        ).pack(side="right", expand=True, padx=5, fill="x")
    
    def register(self):
        """Kayıt işlemi"""
        # Verileri al
        data = {key: entry.get().strip() for key, entry in self.entries.items()}
        
        # Validasyon
        if not all([data['ad_soyad'], data['kullanici_adi'], data['email'], data['telefon'], data['sifre']]):
            messagebox.showwarning("Uyarı", "Lütfen tüm alanları doldurun!")
            return
        
        # Email formatı kontrolü
        if not re.match(r"[^@]+@[^@]+\.[^@]+", data['email']):
            messagebox.showwarning("Uyarı", "Geçerli bir email adresi girin!")
            return
        
        # Telefon formatı kontrolü (basit)
        if not re.match(r"^[0-9]{10,11}$", data['telefon'].replace(" ", "")):
            messagebox.showwarning("Uyarı", "Geçerli bir telefon numarası girin! (10-11 rakam)")
            return
        
        # Şifre kontrolü
        if data['sifre'] != data['sifre_tekrar']:
            messagebox.showwarning("Uyarı", "Şifreler eşleşmiyor!")
            return
        
        if len(data['sifre']) < 6:
            messagebox.showwarning("Uyarı", "Şifre en az 6 karakter olmalıdır!")
            return
        
        # Kullanıcı adı kontrolü
        if self.db.check_username_exists(data['kullanici_adi']):
            messagebox.showwarning("Uyarı", "Bu kullanıcı adı zaten kullanılıyor!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            hashed_password = self.db.hash_password(data['sifre'])
            
            cursor.execute("""
                INSERT INTO Kullanicilar (KullaniciAdi, Sifre, AdSoyad, Email, Telefon, Rol)
                VALUES (?, ?, ?, ?, ?, 'Uye')
            """, data['kullanici_adi'], hashed_password, data['ad_soyad'], 
                 data['email'], data['telefon'])
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Kayıt başarılı! Şimdi giriş yapabilirsiniz.")
            self.destroy()
            
        except Exception as e:
            messagebox.showerror("Hata", f"Kayıt yapılamadı: {str(e)}")


class ForgotPasswordWindow(ctk.CTkToplevel):
    def __init__(self, db):
        super().__init__()
        
        self.db = db
        
        # Pencere ayarları
        self.title("🔑 Şifremi Unuttum")
        self.geometry("450x400")
        self.resizable(False, False)
        
        # Başlık
        title = ctk.CTkLabel(
            self,
            text="🔑 Şifre Sıfırlama",
            font=ctk.CTkFont(size=24, weight="bold")
        )
        title.pack(pady=30)
        
        # Açıklama
        info = ctk.CTkLabel(
            self,
            text="Kullanıcı adınızı ve email adresinizi girin.\nŞifre sıfırlama bağlantısı email'inize gönderilecek.",
            font=ctk.CTkFont(size=12),
            justify="center"
        )
        info.pack(pady=10)
        
        # Form
        form_frame = ctk.CTkFrame(self)
        form_frame.pack(pady=20, padx=40, fill="x")
        
        ctk.CTkLabel(
            form_frame,
            text="Kullanıcı Adı",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(pady=(20, 5), anchor="w", padx=20)
        
        self.username_entry = ctk.CTkEntry(
            form_frame,
            placeholder_text="Kullanıcı adınız",
            height=40
        )
        self.username_entry.pack(padx=20, fill="x")
        
        ctk.CTkLabel(
            form_frame,
            text="Email",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(pady=(15, 5), anchor="w", padx=20)
        
        self.email_entry = ctk.CTkEntry(
            form_frame,
            placeholder_text="Email adresiniz",
            height=40
        )
        self.email_entry.pack(padx=20, fill="x")
        
        # Butonlar
        btn_frame = ctk.CTkFrame(form_frame, fg_color="transparent")
        btn_frame.pack(pady=25, fill="x", padx=20)
        
        ctk.CTkButton(
            btn_frame,
            text="📧 Gönder",
            command=self.send_reset,
            height=45,
            font=ctk.CTkFont(size=14, weight="bold")
        ).pack(side="left", expand=True, padx=5, fill="x")
        
        ctk.CTkButton(
            btn_frame,
            text="❌ İptal",
            command=self.destroy,
            height=45,
            fg_color="gray",
            font=ctk.CTkFont(size=14)
        ).pack(side="right", expand=True, padx=5, fill="x")
    
    def send_reset(self):
        """Şifre sıfırlama işlemi"""
        username = self.username_entry.get().strip()
        email = self.email_entry.get().strip()
        
        if not username or not email:
            messagebox.showwarning("Uyarı", "Lütfen tüm alanları doldurun!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT KullaniciID, Email FROM Kullanicilar 
                WHERE KullaniciAdi = ? AND Email = ? AND AktifMi = 1
            """, username, email)
            
            row = cursor.fetchone()
            
            if not row:
                messagebox.showerror("Hata", "Kullanıcı adı veya email hatalı!")
                return
            
            # Reset token oluştur
            token = self.db.generate_reset_token()
            expire_date = datetime.now() + timedelta(hours=1)
            
            cursor.execute("""
                UPDATE Kullanicilar 
                SET SifreResetToken = ?, TokenSonKullanmaTarihi = ?
                WHERE KullaniciID = ?
            """, token, expire_date, row.KullaniciID)
            
            self.db.get_connection().commit()
            
            # TODO: Email gönderme işlemi burada yapılacak
            # Şimdilik token'ı gösterelim
            messagebox.showinfo(
                "Başarılı", 
                f"Şifre sıfırlama kodu:\n{token}\n\nBu kodu kullanarak şifrenizi sıfırlayabilirsiniz.\n(1 saat geçerli)"
            )
            
            # Yeni şifre girme ekranını aç
            self.destroy()
            reset_window = ResetPasswordWindow(self.db, row.KullaniciID, token)
            
        except Exception as e:
            messagebox.showerror("Hata", f"İşlem başarısız: {str(e)}")


class ResetPasswordWindow(ctk.CTkToplevel):
    def __init__(self, db, user_id, token):
        super().__init__()
        
        self.db = db
        self.user_id = user_id
        self.token = token
        
        # Pencere ayarları
        self.title("🔐 Yeni Şifre Belirle")
        self.geometry("450x400")
        self.resizable(False, False)
        
        # Başlık
        title = ctk.CTkLabel(
            self,
            text="🔐 Yeni Şifre Belirle",
            font=ctk.CTkFont(size=24, weight="bold")
        )
        title.pack(pady=30)
        
        # Form
        form_frame = ctk.CTkFrame(self)
        form_frame.pack(pady=20, padx=40, fill="x")
        
        ctk.CTkLabel(
            form_frame,
            text="Sıfırlama Kodu",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(pady=(20, 5), anchor="w", padx=20)
        
        self.token_entry = ctk.CTkEntry(
            form_frame,
            placeholder_text="Email'inizdeki kodu girin",
            height=40
        )
        self.token_entry.pack(padx=20, fill="x")
        self.token_entry.insert(0, token)  # Geliştirme için otomatik doldur
        
        ctk.CTkLabel(
            form_frame,
            text="Yeni Şifre",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(pady=(15, 5), anchor="w", padx=20)
        
        self.password_entry = ctk.CTkEntry(
            form_frame,
            placeholder_text="Yeni şifreniz",
            show="●",
            height=40
        )
        self.password_entry.pack(padx=20, fill="x")
        
        ctk.CTkLabel(
            form_frame,
            text="Yeni Şifre Tekrar",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(pady=(15, 5), anchor="w", padx=20)
        
        self.password_confirm_entry = ctk.CTkEntry(
            form_frame,
            placeholder_text="Yeni şifreniz (tekrar)",
            show="●",
            height=40
        )
        self.password_confirm_entry.pack(padx=20, fill="x")
        
        # Butonlar
        btn_frame = ctk.CTkFrame(form_frame, fg_color="transparent")
        btn_frame.pack(pady=25, fill="x", padx=20)
        
        ctk.CTkButton(
            btn_frame,
            text="✅ Şifreyi Değiştir",
            command=self.reset_password,
            height=45,
            font=ctk.CTkFont(size=14, weight="bold")
        ).pack(side="left", expand=True, padx=5, fill="x")
        
        ctk.CTkButton(
            btn_frame,
            text="❌ İptal",
            command=self.destroy,
            height=45,
            fg_color="gray",
            font=ctk.CTkFont(size=14)
        ).pack(side="right", expand=True, padx=5, fill="x")
    
    def reset_password(self):
        """Şifreyi sıfırla"""
        token = self.token_entry.get().strip()
        password = self.password_entry.get()
        password_confirm = self.password_confirm_entry.get()
        
        if not token or not password or not password_confirm:
            messagebox.showwarning("Uyarı", "Lütfen tüm alanları doldurun!")
            return
        
        if password != password_confirm:
            messagebox.showwarning("Uyarı", "Şifreler eşleşmiyor!")
            return
        
        if len(password) < 6:
            messagebox.showwarning("Uyarı", "Şifre en az 6 karakter olmalıdır!")
            return
        
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT KullaniciID FROM Kullanicilar 
                WHERE KullaniciID = ? AND SifreResetToken = ? 
                AND TokenSonKullanmaTarihi > GETDATE()
            """, self.user_id, token)
            
            if not cursor.fetchone():
                messagebox.showerror("Hata", "Geçersiz veya süresi dolmuş kod!")
                return
            
            # Yeni şifreyi kaydet
            hashed_password = self.db.hash_password(password)
            cursor.execute("""
                UPDATE Kullanicilar 
                SET Sifre = ?, SifreResetToken = NULL, TokenSonKullanmaTarihi = NULL
                WHERE KullaniciID = ?
            """, hashed_password, self.user_id)
            
            self.db.get_connection().commit()
            messagebox.showinfo("Başarılı", "Şifreniz başarıyla değiştirildi!\nŞimdi giriş yapabilirsiniz.")
            self.destroy()
            
        except Exception as e:
            messagebox.showerror("Hata", f"Şifre sıfırlanamadı: {str(e)}")