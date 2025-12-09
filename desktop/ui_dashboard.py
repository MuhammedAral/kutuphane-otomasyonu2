import customtkinter as ctk
from tkinter import messagebox
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure
import matplotlib
matplotlib.use('TkAgg')

class DashboardFrame(ctk.CTkScrollableFrame):
    def __init__(self, parent, db):
        super().__init__(parent)
        self.db = db
        
        # Başlık
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=20, pady=20)
        
        title = ctk.CTkLabel(
            header,
            text="📊 Dashboard",
            font=ctk.CTkFont(size=28, weight="bold")
        )
        title.pack(side="left")
        
        refresh_btn = ctk.CTkButton(
            header,
            text="🔄 Yenile",
            command=self.refresh_data,
            width=100,
            height=35
        )
        refresh_btn.pack(side="right")
        
        # İstatistik kartları
        stats_frame = ctk.CTkFrame(self, fg_color="transparent")
        stats_frame.pack(fill="x", padx=20, pady=10)
        
        self.stat_cards = {}
        self.create_stat_cards(stats_frame)
        
        # Grafikler bölümü
        charts_frame = ctk.CTkFrame(self, fg_color="transparent")
        charts_frame.pack(fill="both", expand=True, padx=20, pady=10)
        
        # Sol: Kitap türleri grafiği
        left_chart = ctk.CTkFrame(charts_frame)
        left_chart.pack(side="left", fill="both", expand=True, padx=(0, 10))
        
        ctk.CTkLabel(
            left_chart,
            text="📚 Kitap Türleri Dağılımı",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(pady=10)
        
        self.chart_frame_1 = ctk.CTkFrame(left_chart)
        self.chart_frame_1.pack(fill="both", expand=True, padx=10, pady=10)
        
        # Sağ: Ödünç istatistikleri
        right_chart = ctk.CTkFrame(charts_frame)
        right_chart.pack(side="right", fill="both", expand=True, padx=(10, 0))
        
        ctk.CTkLabel(
            right_chart,
            text="📊 Ödünç İstatistikleri",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(pady=10)
        
        self.chart_frame_2 = ctk.CTkFrame(right_chart)
        self.chart_frame_2.pack(fill="both", expand=True, padx=10, pady=10)
        
        # Son aktiviteler
        activity_frame = ctk.CTkFrame(self)
        activity_frame.pack(fill="x", padx=20, pady=20)
        
        ctk.CTkLabel(
            activity_frame,
            text="📋 Son Aktiviteler",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(pady=10, padx=15, anchor="w")
        
        self.activity_text = ctk.CTkTextbox(
            activity_frame,
            height=150,
            font=ctk.CTkFont(size=12)
        )
        self.activity_text.pack(fill="x", padx=15, pady=(0, 15))
        
        # Verileri yükle
        self.refresh_data()
    
    def create_stat_cards(self, parent):
        """İstatistik kartlarını oluştur"""
        stats = [
            ("📚 Toplam Kitap", "total_books", "#3b82f6"),
            ("📖 Ödünçte", "on_loan", "#f59e0b"),
            ("👥 Aktif Üye", "active_members", "#10b981"),
            ("⏰ Gecikmeler", "overdue", "#ef4444")
        ]
        
        for title, key, color in stats:
            card = ctk.CTkFrame(parent, fg_color=color, corner_radius=15)
            card.pack(side="left", fill="both", expand=True, padx=5)
            
            ctk.CTkLabel(
                card,
                text=title,
                font=ctk.CTkFont(size=13),
                text_color="white"
            ).pack(pady=(15, 5))
            
            value_label = ctk.CTkLabel(
                card,
                text="0",
                font=ctk.CTkFont(size=32, weight="bold"),
                text_color="white"
            )
            value_label.pack(pady=(0, 15))
            
            self.stat_cards[key] = value_label
    
    def refresh_data(self):
        """Verileri yenile"""
        try:
            cursor = self.db.get_connection().cursor()
            
            # Toplam kitap sayısı
            cursor.execute("SELECT SUM(StokAdedi) FROM Kitaplar")
            total_books = cursor.fetchone()[0] or 0
            self.stat_cards['total_books'].configure(text=str(total_books))
            
            # Ödünçte olan kitaplar
            cursor.execute("SELECT COUNT(*) FROM OduncIslemleri WHERE Durum = 'Odunc'")
            on_loan = cursor.fetchone()[0] or 0
            self.stat_cards['on_loan'].configure(text=str(on_loan))
            
            # Aktif üyeler
            cursor.execute("SELECT COUNT(*) FROM Kullanicilar WHERE AktifMi = 1 AND Rol = 'Uye'")
            active_members = cursor.fetchone()[0] or 0
            self.stat_cards['active_members'].configure(text=str(active_members))
            
            # Geciken kitaplar
            cursor.execute("""
                SELECT COUNT(*) FROM OduncIslemleri 
                WHERE Durum = 'Odunc' AND BeklenenIadeTarihi < GETDATE()
            """)
            overdue = cursor.fetchone()[0] or 0
            self.stat_cards['overdue'].configure(text=str(overdue))
            
            # Grafikleri güncelle
            self.update_book_types_chart()
            self.update_loan_stats_chart()
            self.update_recent_activity()
            
        except Exception as e:
            messagebox.showerror("Hata", f"Veriler yüklenemedi: {str(e)}")
    
    def update_book_types_chart(self):
        """Kitap türleri pasta grafiği"""
        try:
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT kt.TurAdi, COUNT(k.KitapID) as Sayi
                FROM KitapTurleri kt
                LEFT JOIN Kitaplar k ON kt.TurID = k.TurID
                GROUP BY kt.TurAdi
                HAVING COUNT(k.KitapID) > 0
            """)
            
            rows = cursor.fetchall()
            
            if not rows:
                # Veri yoksa boş grafik göster
                fig = Figure(figsize=(5, 4), dpi=100, facecolor='#2b2b2b')
                ax = fig.add_subplot(111)
                ax.text(0.5, 0.5, 'Henüz kitap eklenmemiş', 
                       ha='center', va='center', fontsize=14, color='white')
                ax.set_facecolor('#2b2b2b')
                ax.axis('off')
            else:
                labels = [row.TurAdi for row in rows]
                sizes = [row.Sayi for row in rows]
                
                fig = Figure(figsize=(5, 4), dpi=100, facecolor='#2b2b2b')
                ax = fig.add_subplot(111)
                
                colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', 
                         '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1']
                
                wedges, texts, autotexts = ax.pie(
                    sizes, 
                    labels=labels, 
                    autopct='%1.1f%%',
                    startangle=90,
                    colors=colors[:len(labels)]
                )
                
                # Metin renklerini ayarla
                for text in texts:
                    text.set_color('white')
                for autotext in autotexts:
                    autotext.set_color('white')
                    autotext.set_weight('bold')
                
                ax.set_facecolor('#2b2b2b')
            
            # Eski grafiği temizle ve yenisini ekle
            for widget in self.chart_frame_1.winfo_children():
                widget.destroy()
            
            canvas = FigureCanvasTkAgg(fig, self.chart_frame_1)
            canvas.draw()
            canvas.get_tk_widget().pack(fill="both", expand=True)
            
        except Exception as e:
            print(f"Grafik hatası: {e}")
    
    def update_loan_stats_chart(self):
        """Ödünç istatistikleri çubuk grafiği"""
        try:
            cursor = self.db.get_connection().cursor()
            
            # Son 7 günün ödünç verme sayıları
            cursor.execute("""
                SELECT 
                    CAST(OduncTarihi AS DATE) as Tarih,
                    COUNT(*) as Sayi
                FROM OduncIslemleri
                WHERE OduncTarihi >= DATEADD(day, -7, GETDATE())
                GROUP BY CAST(OduncTarihi AS DATE)
                ORDER BY Tarih
            """)
            
            rows = cursor.fetchall()
            
            fig = Figure(figsize=(5, 4), dpi=100, facecolor='#2b2b2b')
            ax = fig.add_subplot(111)
            
            if rows:
                dates = [row.Tarih.strftime('%d/%m') for row in rows]
                counts = [row.Sayi for row in rows]
                
                bars = ax.bar(dates, counts, color='#3b82f6', alpha=0.8)
                
                # Bar değerlerini göster
                for bar in bars:
                    height = bar.get_height()
                    ax.text(bar.get_x() + bar.get_width()/2., height,
                           f'{int(height)}',
                           ha='center', va='bottom', color='white', fontweight='bold')
                
                ax.set_xlabel('Tarih', color='white')
                ax.set_ylabel('Ödünç Sayısı', color='white')
                ax.tick_params(colors='white')
            else:
                ax.text(0.5, 0.5, 'Son 7 günde ödünç işlemi yok', 
                       ha='center', va='center', fontsize=12, color='white')
            
            ax.set_facecolor('#2b2b2b')
            ax.spines['bottom'].set_color('white')
            ax.spines['left'].set_color('white')
            ax.spines['top'].set_visible(False)
            ax.spines['right'].set_visible(False)
            
            fig.tight_layout()
            
            # Eski grafiği temizle ve yenisini ekle
            for widget in self.chart_frame_2.winfo_children():
                widget.destroy()
            
            canvas = FigureCanvasTkAgg(fig, self.chart_frame_2)
            canvas.draw()
            canvas.get_tk_widget().pack(fill="both", expand=True)
            
        except Exception as e:
            print(f"Grafik hatası: {e}")
    
    def update_recent_activity(self):
        """Son aktiviteleri göster"""
        try:
            self.activity_text.delete("1.0", "end")
            
            cursor = self.db.get_connection().cursor()
            cursor.execute("""
                SELECT TOP 10
                    k.Baslik,
                    u.AdSoyad,
                    o.OduncTarihi,
                    o.Durum
                FROM OduncIslemleri o
                JOIN Kitaplar k ON o.KitapID = k.KitapID
                JOIN Kullanicilar u ON o.UyeID = u.KullaniciID
                ORDER BY o.OduncTarihi DESC
            """)
            
            rows = cursor.fetchall()
            
            if rows:
                for row in rows:
                    tarih = row.OduncTarihi.strftime('%d/%m/%Y %H:%M')
                    durum_icon = "📤" if row.Durum == "Odunc" else "📥"
                    durum_text = "ödünç aldı" if row.Durum == "Odunc" else "iade etti"
                    
                    activity = f"{durum_icon} {row.AdSoyad} - '{row.Baslik}' {durum_text} ({tarih})\n"
                    self.activity_text.insert("end", activity)
            else:
                self.activity_text.insert("end", "Henüz aktivite bulunmuyor.")
            
            self.activity_text.configure(state="disabled")
            
        except Exception as e:
            print(f"Aktivite hatası: {e}")