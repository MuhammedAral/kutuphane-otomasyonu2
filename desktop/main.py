import customtkinter as ctk
from database import init_database
from ui_kitaplar_enhanced import KitaplarEnhancedFrame
from ui_uyeler import UyelerFrame
from ui_odunc import OduncFrame
import tkinter.messagebox as messagebox
from PIL import Image
import os

class KutuphaneApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        
        # Uygulama dizini
        self.app_dir = os.path.dirname(os.path.abspath(__file__))
        
        # Pencere ayarları
        self.title("Kütüphane Otomasyon Sistemi")
        self.geometry("1400x800")
        self.minsize(1200, 700)
        
        # Logo ayarla (pencere ve görev çubuğu için)
        self.set_app_icon()
        
        # Tema
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("blue")
        
        # Aktif buton takibi
        self.active_button = None
        
        # Veritabanı bağlantısı
        try:
            self.db = init_database()
            print("✅ Veritabanı bağlantısı başarılı!")
        except Exception as e:
            messagebox.showerror("Hata", f"Veritabanı bağlantısı başarısız!\n\n{str(e)}\n\nDocker container'ın çalıştığından emin olun.")
            self.destroy()
            return
        
        # Ana container
        self.main_container = ctk.CTkFrame(self, fg_color="transparent")
        self.main_container.pack(fill="both", expand=True)
        
        # Modern sidebar oluştur
        self.create_sidebar()
        
        # Ana içerik alanı
        self.content_area = ctk.CTkFrame(self.main_container, fg_color="transparent")
        self.content_area.pack(side="right", fill="both", expand=True, padx=15, pady=15)
        
        # İçerik çerçevesi (gölgeli)
        self.main_frame = ctk.CTkFrame(self.content_area, corner_radius=15)
        self.main_frame.pack(fill="both", expand=True)
        
        # İlk ekran
        self.current_frame = None
        self.show_kitaplar()
    
    def set_app_icon(self):
        """Uygulama ikonunu ayarla (pencere ve görev çubuğu için)"""
        try:
            icon_path = os.path.join(self.app_dir, "assets", "logo.png")
            if os.path.exists(icon_path):
                from PIL import Image
                img = Image.open(icon_path)
                
                # RGBA moduna çevir (transparan destek için)
                if img.mode != 'RGBA':
                    img = img.convert('RGBA')
                
                # .ico dosyası oluştur (çoklu boyut)
                ico_path = os.path.join(self.app_dir, "assets", "logo.ico")
                
                # Farklı boyutlarda ikonlar
                sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
                icons = []
                for size in sizes:
                    resized = img.resize(size, Image.Resampling.LANCZOS)
                    icons.append(resized)
                
                # ICO olarak kaydet
                icons[0].save(ico_path, format='ICO', sizes=[(s, s) for s in [16, 32, 48, 64, 128, 256]], append_images=icons[1:])
                
                # Pencere ikonu ayarla
                self.iconbitmap(ico_path)
                
                # Görev çubuğu için Windows API
                try:
                    import ctypes
                    myappid = 'kutuphane.otomasyon.sistemi.2.0'
                    ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(myappid)
                except:
                    pass
                
                print("✅ Uygulama ikonu ayarlandı!")
        except Exception as e:
            print(f"⚠️ İkon ayarlanamadı: {e}")
    
    def create_sidebar(self):
        """Modern sidebar oluştur"""
        # Sidebar container
        self.sidebar = ctk.CTkFrame(
            self.main_container, 
            width=280, 
            corner_radius=0,
            fg_color="#1a1a2e"
        )
        self.sidebar.pack(side="left", fill="y", padx=0, pady=0)
        self.sidebar.pack_propagate(False)
        
        # Logo ve başlık alanı
        logo_frame = ctk.CTkFrame(self.sidebar, fg_color="transparent")
        logo_frame.pack(fill="x", padx=20, pady=30)
        
        # Logo resmi
        try:
            logo_path = os.path.join(self.app_dir, "assets", "logo.png")
            if os.path.exists(logo_path):
                logo_image = ctk.CTkImage(
                    light_image=Image.open(logo_path),
                    dark_image=Image.open(logo_path),
                    size=(60, 60)
                )
                logo_label = ctk.CTkLabel(logo_frame, image=logo_image, text="")
                logo_label.pack(pady=(0, 15))
        except Exception as e:
            print(f"Logo yüklenemedi: {e}")
        
        # Başlık
        title_label = ctk.CTkLabel(
            logo_frame,
            text="Kütüphane Sistemi",
            font=ctk.CTkFont(family="Segoe UI", size=22, weight="bold"),
            text_color="#ffffff"
        )
        title_label.pack()
        
        # Alt başlık
        subtitle_label = ctk.CTkLabel(
            logo_frame,
            text="Yönetim Paneli",
            font=ctk.CTkFont(family="Segoe UI", size=12),
            text_color="#6b7280"
        )
        subtitle_label.pack(pady=(5, 0))
        
        # Ayırıcı çizgi
        separator = ctk.CTkFrame(self.sidebar, height=1, fg_color="#2d2d44")
        separator.pack(fill="x", padx=20, pady=20)
        
        # Menü başlığı
        menu_title = ctk.CTkLabel(
            self.sidebar,
            text="MENÜ",
            font=ctk.CTkFont(size=11, weight="bold"),
            text_color="#6b7280",
            anchor="w"
        )
        menu_title.pack(fill="x", padx=25, pady=(10, 15))
        
        # Menü butonları container
        menu_frame = ctk.CTkFrame(self.sidebar, fg_color="transparent")
        menu_frame.pack(fill="x", padx=15)
        
        # Menü butonları
        self.btn_kitaplar = self.create_menu_button(
            menu_frame, 
            "📚  Kitap Yönetimi", 
            self.show_kitaplar,
            "Kitapları görüntüle ve düzenle"
        )
        
        self.btn_uyeler = self.create_menu_button(
            menu_frame, 
            "👥  Üye Yönetimi", 
            self.show_uyeler,
            "Üyeleri görüntüle ve düzenle"
        )
        
        self.btn_odunc = self.create_menu_button(
            menu_frame, 
            "🔄  Ödünç İşlemleri", 
            self.show_odunc,
            "Ödünç ve iade işlemleri"
        )
        
        # Alt kısım - Bilgi ve çıkış
        bottom_frame = ctk.CTkFrame(self.sidebar, fg_color="transparent")
        bottom_frame.pack(side="bottom", fill="x", padx=15, pady=20)
        
        # Ayırıcı
        separator2 = ctk.CTkFrame(bottom_frame, height=1, fg_color="#2d2d44")
        separator2.pack(fill="x", pady=(0, 15))
        
        # Versiyon bilgisi
        version_label = ctk.CTkLabel(
            bottom_frame,
            text="v2.0.0 • Modern Edition",
            font=ctk.CTkFont(size=10),
            text_color="#4b5563"
        )
        version_label.pack(pady=(0, 10))
        
        # Çıkış butonu
        self.btn_cikis = ctk.CTkButton(
            bottom_frame,
            text="🚪  Çıkış Yap",
            command=self.on_closing,
            fg_color="#dc2626",
            hover_color="#b91c1c",
            height=45,
            corner_radius=10,
            font=ctk.CTkFont(size=14, weight="bold")
        )
        self.btn_cikis.pack(fill="x")
    
    def create_menu_button(self, parent, text, command, tooltip=""):
        """Stil uygulanmış menü butonu oluştur"""
        btn_frame = ctk.CTkFrame(parent, fg_color="transparent")
        btn_frame.pack(fill="x", pady=5)
        
        btn = ctk.CTkButton(
            btn_frame,
            text=text,
            command=lambda: self.on_menu_click(command, btn),
            height=50,
            corner_radius=12,
            font=ctk.CTkFont(family="Segoe UI", size=14),
            fg_color="transparent",
            hover_color="#2d2d44",
            text_color="#e5e7eb",
            anchor="w"
        )
        btn.pack(fill="x", padx=5)
        
        return btn
    
    def on_menu_click(self, command, button):
        """Menü butonuna tıklandığında"""
        # Önceki aktif butonu sıfırla
        if self.active_button:
            self.active_button.configure(fg_color="transparent")
        
        # Yeni aktif butonu ayarla
        button.configure(fg_color="#3b82f6")
        self.active_button = button
        
        # Komutu çalıştır
        command()
    
    def clear_main_frame(self):
        """Ana frame'i temizle"""
        if self.current_frame:
            self.current_frame.destroy()
    
    def show_kitaplar(self):
        """Kitaplar ekranını göster"""
        self.clear_main_frame()
        self.current_frame = KitaplarEnhancedFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
        
        # İlk açılışta kitaplar butonu aktif olsun
        if hasattr(self, 'btn_kitaplar') and not self.active_button:
            self.btn_kitaplar.configure(fg_color="#3b82f6")
            self.active_button = self.btn_kitaplar
    
    def show_uyeler(self):
        """Üyeler ekranını göster"""
        self.clear_main_frame()
        self.current_frame = UyelerFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_odunc(self):
        """Ödünç işlemleri ekranını göster"""
        self.clear_main_frame()
        self.current_frame = OduncFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def on_closing(self):
        """Pencere kapanırken"""
        if messagebox.askokcancel("Çıkış", "Uygulamadan çıkmak istiyor musunuz?"):
            self.db.close()
            self.destroy()

if __name__ == "__main__":
    app = KutuphaneApp()
    app.protocol("WM_DELETE_WINDOW", app.on_closing)
    app.mainloop()