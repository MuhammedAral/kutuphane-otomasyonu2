// ============================================
// KÜTÜPHANE WEB SİTESİ - API İSTEKLERİ
// ============================================

const API_BASE = '/api';

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

        // Token payload'ını decode et
        const tokenParts = data.token.split('.');
        if (tokenParts.length !== 3) {
            throw new Error('Geçersiz token formatı');
        }

        const payload = JSON.parse(atob(tokenParts[1]));

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
