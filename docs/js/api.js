// ============================================
// KÜTÜPHANE WEB SİTESİ - API İSTEKLERİ
// ============================================

const API_BASE = 'https://kutuphane-api-production.up.railway.app/api';

// Token işlemleri
const Auth = {
    getToken: () => localStorage.getItem('kutuphane_token'),
    setToken: (token) => localStorage.setItem('kutuphane_token', token),
    removeToken: () => localStorage.removeItem('kutuphane_token'),

    getUser: () => {
        const user = localStorage.getItem('kutuphane_user');
        try {
            return user ? JSON.parse(user) : null;
        } catch {
            return null;
        }
    },
    setUser: (user) => localStorage.setItem('kutuphane_user', JSON.stringify(user)),
    removeUser: () => localStorage.removeItem('kutuphane_user'),

    isLoggedIn: () => {
        const token = Auth.getToken();
        const user = Auth.getUser();
        return token && user && user.id;
    },

    logout: () => {
        Auth.removeToken();
        Auth.removeUser();
        window.location.href = '/login.html';
    },

    requireAuth: () => {
        if (!Auth.isLoggedIn()) {
            window.location.href = '/login.html';
            return false;
        }
        return true;
    },

    requireRole: (role) => {
        const user = Auth.getUser();
        if (!user || user.role !== role) {
            Auth.logout();
            return false;
        }
        return true;
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

            // Unauthorized - çıkış yap
            if (response.status === 401) {
                Auth.logout();
                return null;
            }

            // Boş response kontrolü
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

        // User bilgilerini çıkar (farklı claim formatlarını dene)
        const user = {
            id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
                || payload.nameid || payload.sub || payload.id,
            name: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
                || payload.unique_name || payload.name,
            role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
                || payload.role
        };

        // Validasyon
        if (!user.id || !user.name || !user.role) {
            console.error('Token payload:', payload);
            throw new Error('Kullanıcı bilgileri alınamadı');
        }

        Auth.setToken(data.token);
        Auth.setUser(user);

        return { token: data.token, user };
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

    // Ödünç işlemleri - üyeye özel
    async getOdunclerim() {
        const user = Auth.getUser();
        if (!user || !user.id) return [];
        return await this.request(`/odunc/uye/${user.id}`) || [];
    },

    // Tüm ödünç işlemleri (admin için)
    async getAllOdunc() {
        return await this.request('/odunc') || [];
    },

    // Üye bilgileri
    async getProfilBilgileri() {
        const user = Auth.getUser();
        if (!user || !user.id) return null;
        return await this.request(`/uyeler/${user.id}`);
    },

    // Tüm üyeler (admin için)
    async getUyeler() {
        return await this.request('/uyeler') || [];
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
    },

    // Profil güncelle (telefon, e-posta) - sadece üyeler için
    async profilGuncelle(telefon, email) {
        const user = Auth.getUser();
        if (!user || !user.id) throw new Error('Giriş yapmalısınız');

        return await this.request(`/uyeler/${user.id}/profil`, {
            method: 'PUT',
            body: JSON.stringify({
                Telefon: telefon || null,
                Email: email || null
            })
        });
    },

    // Tüm değerlendirmeleri getir (Admin için)
    async getAllDegerlendirmeler() {
        return await this.request('/degerlendirmeler') || [];
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

    showLoading: (container) => {
        if (!container) return;
        container.innerHTML = `
            <div class="loading">
                <div class="spinner"></div>
                <span>Yükleniyor...</span>
            </div>
        `;
    },

    showEmpty: (container, message = 'Kayıt bulunamadı', icon = '📭') => {
        if (!container) return;
        container.innerHTML = `
            <div class="empty-state">
                <div class="icon">${icon}</div>
                <h3>${message}</h3>
            </div>
        `;
    },

    showError: (container, message = 'Bir hata oluştu') => {
        if (!container) return;
        container.innerHTML = `
            <div class="empty-state">
                <div class="icon">⚠️</div>
                <h3>${message}</h3>
                <p>Lütfen sayfayı yenileyin veya daha sonra tekrar deneyin.</p>
            </div>
        `;
    }
};

// Sayfa yüklendiğinde sidebar kullanıcı bilgilerini güncelle
document.addEventListener('DOMContentLoaded', () => {
    const userNameEl = document.getElementById('user-name');
    const userAvatarEl = document.getElementById('user-avatar');
    const userRoleEl = document.getElementById('user-role');
    const sidebarUsernameEl = document.getElementById('sidebar-username');

    const user = Auth.getUser();

    if (userNameEl && user) {
        userNameEl.textContent = user.name || 'Kullanıcı';
    }

    if (userAvatarEl && user) {
        userAvatarEl.textContent = Utils.getInitials(user.name);
    }

    if (userRoleEl && user) {
        userRoleEl.textContent = user.role === 'Yonetici' ? 'Yönetici' : 'Üye';
    }

    // Sidebar başlığında kullanıcı adını göster
    if (sidebarUsernameEl && user) {
        sidebarUsernameEl.textContent = user.name || 'Yönetim';
    }

    // Aktif sayfa işaretleme
    const currentPage = window.location.pathname.split('/').pop() || 'index.html';
    document.querySelectorAll('.nav-item').forEach(item => {
        const href = item.getAttribute('href');
        if (href === currentPage || href === '/' + currentPage) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
});
