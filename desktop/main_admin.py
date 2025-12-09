import customtkinter as ctk
from tkinter import messagebox
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure

class AdminWindow(ctk.CTk):
    def __init__(self, db, user_data):
        super().__init__()
        
        self.db = db
        self.user_data = user_data
        
        # Pencere ayarları
        self.title(f"📚 Kütüphane Yönetim - {user_data['ad_soyad']}")
        self.geometry("1400x800")
        
        # Tema
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("blue")
        
        # Sol menü
        self.sidebar = ctk.CTkFrame(self, width=250, corner_radius=0)
        self.sidebar.pack(side="left", fill="y")
        self.sidebar.pack_propagate(False)
        
        # Profil bölümü
        profile_frame = ctk.CTkFrame(self.sidebar, fg_color="transparent")
        profile_frame.pack(pady=20, padx=10, fill="x")
        
        profile_icon = ctk.CTkLabel(
            profile_frame,
            text="👨‍💼",
            font=ctk.CTkFont(size=40)
        )
        profile_icon.pack()
        
        profile_name = ctk.CTkLabel(
            profile_frame,
            text=user_data['ad_soyad'],
            font=ctk.CTkFont(size=14, weight="bold")
        )
        profile_name.pack(pady=5)
        
        profile_role = ctk.CTkLabel(
            profile_frame,
            text="🔐 Yönetici",
            font=ctk.CTkFont(size=11),
            text_color="gold"
        )
        profile_role.pack()
        
        # Menü başlığı
        menu_label = ctk.CTkLabel(
            self.sidebar,
            text="Menü",
            font=ctk.CTkFont(size=16, weight="bold")
        )
        menu_label.pack(pady=(20, 10), padx=20, anchor="w")
        
        # Menü butonları
        menu_items = [
            ("📊 Dashboard", self.show_dashboard),
            ("📚 Kitap Yönetimi", self.show_books),
            ("👥 Üye Yönetimi", self.show_members),
            ("🔄 Ödünç İşlemleri", self.show_loans),
            ("📨 İstekler", self.show_requests),
            ("📧 Bildirimler", self.show_notifications),
            ("⚙️ Ayarlar", self.show_settings),
        ]
        
        self.menu_buttons = {}
        for text, command in menu_items:
            btn = ctk.CTkButton(
                self.sidebar,
                text=text,
                command=command,
                height=45,
                font=ctk.CTkFont(size=13),
                anchor="w",
                fg_color="transparent",
                text_color=("gray10", "gray90"),
                hover_color=("gray70", "gray30")
            )
            btn.pack(pady=5, padx=10, fill="x")
            self.menu_buttons[text] = btn
        
        # Çıkış butonu
        logout_btn = ctk.CTkButton(
            self.sidebar,
            text="🚪 Çıkış Yap",
            command=self.logout,
            height=45,
            font=ctk.CTkFont(size=13),
            fg_color="red",
            hover_color="darkred"
        )
        logout_btn.pack(side="bottom", pady=20, padx=10, fill="x")
        
        # Ana içerik alanı
        self.main_frame = ctk.CTkFrame(self, corner_radius=0)
        self.main_frame.pack(side="right", fill="both", expand=True)
        
        # İlk ekran
        self.current_frame = None
        self.show_dashboard()
        
    def highlight_button(self, button_text):
        """Aktif menü butonunu vurgula"""
        for text, btn in self.menu_buttons.items():
            if text == button_text:
                btn.configure(fg_color=("gray75", "gray25"))
            else:
                btn.configure(fg_color="transparent")
    
    def clear_main_frame(self):
        """Ana içeriği temizle"""
        if self.current_frame:
            self.current_frame.destroy()
    
    def show_dashboard(self):
        """Dashboard göster"""
        self.clear_main_frame()
        self.highlight_button("📊 Dashboard")
        from ui_dashboard import DashboardFrame
        self.current_frame = DashboardFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_books(self):
        """Kitap yönetimi göster"""
        self.clear_main_frame()
        self.highlight_button("📚 Kitap Yönetimi")
        from ui_kitaplar_enhanced import KitaplarEnhancedFrame
        self.current_frame = KitaplarEnhancedFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_members(self):
        """Üye yönetimi göster"""
        self.clear_main_frame()
        self.highlight_button("👥 Üye Yönetimi")
        from ui_uyeler_enhanced import UyelerEnhancedFrame
        self.current_frame = UyelerEnhancedFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_loans(self):
        """Ödünç işlemleri göster"""
        self.clear_main_frame()
        self.highlight_button("🔄 Ödünç İşlemleri")
        from ui_odunc_enhanced import OduncEnhancedFrame
        self.current_frame = OduncEnhancedFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_requests(self):
        """İstekler göster"""
        self.clear_main_frame()
        self.highlight_button("📨 İstekler")
        from ui_requests import RequestsFrame
        self.current_frame = RequestsFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_notifications(self):
        """Bildirimler göster"""
        self.clear_main_frame()
        self.highlight_button("📧 Bildirimler")
        from ui_notifications import NotificationsFrame
        self.current_frame = NotificationsFrame(self.main_frame, self.db, self.user_data)
        self.current_frame.pack(fill="both", expand=True)
    
    def show_settings(self):
        """Ayarlar göster"""
        self.clear_main_frame()
        self.highlight_button("⚙️ Ayarlar")
        from ui_settings import SettingsFrame
        self.current_frame = SettingsFrame(self.main_frame, self.db)
        self.current_frame.pack(fill="both", expand=True)
    
    def logout(self):
        """Çıkış yap"""
        if messagebox.askyesno("Çıkış", "Çıkış yapmak istediğinizden emin misiniz?"):
            self.destroy()


class DummyMemberWindow:
    """Geliştirme aşaması için dummy member window"""
    def __init__(self, db, user_data):
        self.window = ctk.CTkToplevel()
        self.window.title("Üye Ekranı")
        self.window.geometry("800x600")
        
        label = ctk.CTkLabel(
            self.window,
            text=f"Üye Ekranı\n{user_data['ad_soyad']}\n\nBu ekran yakında geliştirilecek...",
            font=ctk.CTkFont(size=20)
        )
        label.pack(expand=True)