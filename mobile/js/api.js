// ============================================
// KÜTÜPHANE MOBİL UYGULAMA - API İSTEKLERİ
// ============================================

const API_BASE = 'http://localhost:5000/api';

// Token işlemleri
const Auth = {
    getToken: () => localStorage.getItem('kutuphane_mobile_token'),
    setToken: (token) => localStorage.setItem('kutuphane_mobile_token', token),
    removeToken: () => localStorage.removeItem('kutuphane_mobile_token'),

    getUser: () => {
        const user = localStorage.getItem('kutuphane_mobile_user');
        try {
            return user ? JSON.parse(user) : null;
        } catch {
            return null;
        }
    },
    setUser: (user) => localStorage.setItem('kutuphane_mobile_user', JSON.stringify(user)),
    removeUser: () => localStorage.removeItem('kutuphane_mobile_user'),

    isLoggedIn: () => {
        const token = Auth.getToken();
        const user = Auth.getUser();
        return token && user && user.id;
    },

    logout: () => {
        Auth.removeToken();
        Auth.removeUser();
    }
};

// API istekleri
const api = {
    async request(endpoint, options = {}) {
        const token = Auth.getToken();

        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...(token && { 'Authorization': `Bearer ${token}` })
            },
            ...options
        };

        try {
            const response = await fetch(`${API_BASE}${endpoint}`, config);

            // Unauthorized
            if (response.status === 401) {
                Auth.logout();
                showLoginPage();
                return null;
            }

            const text = await response.text();
            if (!text) return null;

            const data = JSON.parse(text);

            if (!response.ok) {
                throw new Error(data.message || 'Bir hata oluştu');
            }

            return data;
        } catch (error) {
            console.error('API Hatası:', error);
            throw error;
        }
    },

    // Giriş
    async login(kullaniciAdi, sifre) {
        const response = await fetch(`${API_BASE}/giris`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ KullaniciAdi: kullaniciAdi, Sifre: sifre })
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Kullanıcı adı veya şifre hatalı!');
            }
            throw new Error('Giriş başarısız');
        }

        const data = await response.json();

        // Token payload'ını decode et (UTF-8 uyumlu)
        const tokenParts = data.token.split('.');
        if (tokenParts.length !== 3) {
            throw new Error('Geçersiz token formatı');
        }

        // Base64 URL decode ve UTF-8 karakter desteği
        const base64Url = tokenParts[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const payload = JSON.parse(jsonPayload);

        const user = {
            id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
                || payload.nameid || payload.sub || payload.id,
            name: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
                || payload.unique_name || payload.name,
            role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
                || payload.role
        };

        if (!user.id || !user.name || !user.role) {
            throw new Error('Kullanıcı bilgileri alınamadı');
        }

        Auth.setToken(data.token);
        Auth.setUser(user);

        return { token: data.token, user };
    },

    // Kayıt ol
    async register(kullaniciAdi, adSoyad, email, telefon, sifre) {
        const response = await fetch(`${API_BASE}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                KullaniciAdi: kullaniciAdi,
                AdSoyad: adSoyad,
                Email: email,
                Telefon: telefon || null,
                Sifre: sifre
            })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Kayıt başarısız');
        }

        return data;
    },

    // E-posta doğrula
    async verifyEmail(userId, kod) {
        const response = await fetch(`${API_BASE}/auth/verify-email`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                UserId: userId,
                Kod: kod
            })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Doğrulama başarısız');
        }

        return data;
    },

    // Kitaplar
    async getKitaplar() {
        return await this.request('/kitaplar') || [];
    },

    async getKitap(id) {
        return await this.request(`/kitaplar/${id}`);
    },

    // Türler
    async getTurler() {
        return await this.request('/turler') || [];
    },

    // Ödünç işlemleri
    async getOdunclerim() {
        const user = Auth.getUser();
        if (!user || !user.id) return [];

        // Admin ise tüm ödünçleri getir, değilse sadece kendi ödünçlerini
        if (user.role === 'Yonetici') {
            return await this.request('/odunc') || [];
        }
        return await this.request(`/odunc/uye/${user.id}`) || [];
    },

    // İstatistikler
    async getIstatistikler() {
        return await this.request('/istatistikler') || {
            toplamKitap: 0,
            toplamUye: 0,
            oduncteKitap: 0,
            gecikenKitap: 0
        };
    },

    // Profil bilgileri
    async getProfilBilgileri() {
        const user = Auth.getUser();
        if (!user || !user.id) return null;
        return await this.request(`/uyeler/${user.id}`);
    },

    // Değerlendirmeler
    async getKitapDegerlendirmeleri(kitapId) {
        return await this.request(`/kitaplar/${kitapId}/degerlendirmeler`) || [];
    },

    async getKitapPuan(kitapId) {
        return await this.request(`/kitaplar/${kitapId}/puan`) || { degerlendirmeSayisi: 0, ortalamaPuan: 0 };
    },

    async degerlendirmeEkle(kitapId, puan, yorum) {
        const user = Auth.getUser();
        if (!user || !user.id) throw new Error('Giriş yapmalısınız');

        return await this.request('/degerlendirmeler', {
            method: 'POST',
            body: JSON.stringify({
                KitapID: kitapId,
                UyeID: user.id,
                Puan: puan,
                Yorum: yorum
            })
        });
    },

    async degerlendirmeSil(id) {
        return await this.request(`/degerlendirmeler/${id}`, {
            method: 'DELETE'
        });
    }
};

// Yardımcı fonksiyonlar
const Utils = {
    formatDate: (dateStr) => {
        if (!dateStr) return '-';
        const date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    },

    getInitials: (name) => {
        if (!name) return '?';
        return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
    },

    isOverdue: (dueDate) => {
        if (!dueDate) return false;
        return new Date(dueDate) < new Date();
    },

    daysRemaining: (dueDate) => {
        if (!dueDate) return 0;
        const diff = new Date(dueDate) - new Date();
        return Math.ceil(diff / (1000 * 60 * 60 * 24));
    }
};

// Toast bildirimi
function showToast(message, type = 'info') {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.className = `toast ${type} show`;

    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}

function showLoginPage() {
    document.getElementById('login-page').style.display = 'block';
}
