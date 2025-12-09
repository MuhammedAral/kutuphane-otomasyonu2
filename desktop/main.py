import customtkinter as ctk
from database import init_database
from ui_kitaplar import KitaplarFrame
from ui_uyeler import UyelerFrame
from ui_odunc import OduncFrame
import tkinter.messagebox as messagebox

class KutuphaneApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        
        # Pencere ayarları
        self.title("📚 Kütüphane Otomasyon Sistemi")
        self.geometry("1200x700")
        
        # Tema
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("blue")
        
        # Veritabanı bağlantısı
        try:
            self.db = init_database()
            print("✅ Veritabanı bağlantısı başarılı!")
        except Exception as e:
            messagebox.showerror("Hata", f"Veritabanı bağlantısı başarısız!\n\n{str(e)}\n\nDocker container'ın çalıştığından emin olun.")
            self.destroy()
            return
        
        # Sol menü
        self.sidebar = ctk.CTkFrame(self, width=200, corner_radius=0)
        self.sidebar.pack(side="left", fill="y", padx=0, pady=0)
        
        # Başlık
        self.logo_label = ctk.CTkLabel(
            self.sidebar, 
            text="📚 Kütüphane\nSistemi", 
            font=ctk.CTkFont(size=20, weight="bold")
        )
        self.logo_label.pack(pady=30)
        
        # Menü butonları
        self.btn_kitaplar = ctk.CTkButton(
            self.sidebar,
            text="📖 Kitaplar",
            command=self.show_kitaplar,
            height=40,
            font=ctk.CTkFont(size=14)
        )
        self.btn_kitaplar.pack(pady=10, padx=20, fill="x")
        
        self.btn_uyeler = ctk.CTkButton(
            self.sidebar,
            text="👥 Üyeler",
            command=self.show_uyeler,
            height=40,
            font=ctk.CTkFont(size=14)
        )
        self.btn_uyeler.pack(pady=10, padx=20, fill="x")
        
        self.btn_odunc = ctk.CTkButton(
            self.sidebar,
            text="🔄 Ödünç İşlemleri",
            command=self.show_odunc,
            height=40,
            font=ctk.CTkFont(size=14)
        )
        self.btn_odunc.pack(pady=10, padx=20, fill="x")
        
        # Çıkış butonu (en altta)
        self.btn_cikis = ctk.CTkButton(
            self.sidebar,
            text="🚪 Çıkış",
            command=self.quit,
            fg_color="red",
            hover_color="darkred",
            height=40,
            font=ctk.CTkFont(size=14)
        )
        self.btn_cikis.pack(side="bottom", pady=20, padx=20, fill="x")
        
        # Ana içerik alanı
        self.main_frame = ctk.CTkFrame(self)
        self.main_frame.pack(side="right", fill="both", expand=True, padx=10, pady=10)
        
        # İlk ekran
        self.current_frame = None
        self.show_kitaplar()
    
    def clear_main_frame(self):
        """Ana frame'i temizle"""
        if self.current_frame:
            self.current_frame.destroy()
    
    def show_kitaplar(self):
        """Kitaplar ekranını göster"""
        self.clear_main_frame()
        self.current_frame = KitaplarFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
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