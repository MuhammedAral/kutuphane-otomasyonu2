// ============================================
// KÜTÜPHANE MOBİL UYGULAMA - ANA UYGULAMA
// ============================================

// Global değişkenler
let allKitaplar = [];
let allTurler = [];
let allOdunc = [];
let currentPage = 'home';
let selectedRating = 0;

// Sayfa yüklendiğinde
document.addEventListener('DOMContentLoaded', () => {
    initApp();
});

async function initApp() {
    // Splash ekranı
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Giriş kontrolü
    if (Auth.isLoggedIn()) {
        showMainApp();
    } else {
        showLogin();
    }

    // Splash'ı gizle
    document.getElementById('splash-screen').classList.add('hidden');

    // Event listener'ları ayarla
    setupEventListeners();
}

function setupEventListeners() {
    // Login formu
    document.getElementById('login-form').addEventListener('submit', handleLogin);

    // Kayıt ol linki
    document.getElementById('register-link').addEventListener('click', (e) => {
        e.preventDefault();
        showRegister();
    });

    // Kayıt formu
    document.getElementById('register-form').addEventListener('submit', handleRegister);

    // Giriş'e geri dön linki
    document.getElementById('back-to-login').addEventListener('click', (e) => {
        e.preventDefault();
        showLogin();
    });

    // Doğrulama formu
    document.getElementById('verify-form').addEventListener('submit', handleVerify);

    // Kayıt'a geri dön linki
    document.getElementById('back-to-register').addEventListener('click', (e) => {
        e.preventDefault();
        showRegister();
    });

    // Alt navigasyon
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', () => {
            const page = item.dataset.page;
            navigateTo(page);
        });
    });

    // Arama
    const searchInput = document.getElementById('search-input');
    const searchClear = document.getElementById('search-clear');

    searchInput.addEventListener('input', debounce(() => {
        filterBooks();
        searchClear.style.display = searchInput.value ? 'block' : 'none';
    }, 300));

    searchClear.addEventListener('click', () => {
        searchInput.value = '';
        searchClear.style.display = 'none';
        filterBooks();
    });

    // Ödünç sekmeleri
    document.querySelectorAll('.loans-tabs .tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.loans-tabs .tab-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            renderLoans(btn.dataset.tab);
        });
    });
}

// Debounce fonksiyonu
function debounce(func, wait) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), wait);
    };
}

// ============================================
// GİRİŞ İŞLEMLERİ
// ============================================

function showLogin() {
    document.getElementById('login-page').style.display = 'block';
    document.getElementById('register-page').style.display = 'none';
    document.getElementById('verify-page').style.display = 'none';
    document.getElementById('main-app').style.display = 'none';
}

// Kayıt ol sayfasını göster
function showRegister() {
    document.getElementById('login-page').style.display = 'none';
    document.getElementById('register-page').style.display = 'block';
    document.getElementById('verify-page').style.display = 'none';
    document.getElementById('main-app').style.display = 'none';
}

// Doğrulama sayfasını göster
let pendingUserId = null;
let pendingEmail = '';

function showVerify(userId, email) {
    pendingUserId = userId;
    pendingEmail = email;
    document.getElementById('login-page').style.display = 'none';
    document.getElementById('register-page').style.display = 'none';
    document.getElementById('verify-page').style.display = 'block';
    document.getElementById('main-app').style.display = 'none';
    document.getElementById('verify-email-text').textContent = `${email} adresine gönderilen kodu girin`;
}

// Kayıt işlemi
async function handleRegister(e) {
    e.preventDefault();

    const username = document.getElementById('register-username').value.trim();
    const fullname = document.getElementById('register-fullname').value.trim();
    const email = document.getElementById('register-email').value.trim();
    const phone = document.getElementById('register-phone').value.trim();
    const password = document.getElementById('register-password').value;
    const passwordConfirm = document.getElementById('register-password-confirm').value;
    const errorEl = document.getElementById('register-error');
    const btn = e.target.querySelector('button');
    const btnText = btn.querySelector('.btn-text');
    const btnLoader = btn.querySelector('.btn-loader');

    // Validasyonlar
    if (!username || !fullname || !email || !password) {
        showError(errorEl, 'Tüm alanları doldurunuz.');
        return;
    }

    if (!email.endsWith('@gmail.com')) {
        showError(errorEl, 'Sadece @gmail.com uzantılı e-posta adresleri kabul edilir.');
        return;
    }

    if (password.length < 6) {
        showError(errorEl, 'Şifre en az 6 karakter olmalıdır.');
        return;
    }

    if (password !== passwordConfirm) {
        showError(errorEl, 'Şifreler eşleşmiyor.');
        return;
    }

    // Loading durumu
    btn.disabled = true;
    btnText.style.display = 'none';
    btnLoader.style.display = 'block';
    errorEl.classList.remove('show');

    try {
        const result = await api.register(username, fullname, email, phone, password);
        showToast('Kayıt başarılı! E-postanızı kontrol edin. 📧', 'success');
        showVerify(result.userId, email);
    } catch (error) {
        showError(errorEl, error.message);
    } finally {
        btn.disabled = false;
        btnText.style.display = 'block';
        btnLoader.style.display = 'none';
    }
}

// Doğrulama işlemi
async function handleVerify(e) {
    e.preventDefault();

    const code = document.getElementById('verify-code').value.trim();
    const errorEl = document.getElementById('verify-error');
    const btn = e.target.querySelector('button');
    const btnText = btn.querySelector('.btn-text');
    const btnLoader = btn.querySelector('.btn-loader');

    if (!code || code.length !== 6) {
        showError(errorEl, '6 haneli doğrulama kodunu girin.');
        return;
    }

    if (!pendingUserId) {
        showError(errorEl, 'Geçersiz işlem. Lütfen tekrar kayıt olun.');
        return;
    }

    // Loading durumu
    btn.disabled = true;
    btnText.style.display = 'none';
    btnLoader.style.display = 'block';
    errorEl.classList.remove('show');

    try {
        await api.verifyEmail(pendingUserId, code);
        showToast('Hesabınız doğrulandı! Giriş yapabilirsiniz. 🎉', 'success');
        pendingUserId = null;
        pendingEmail = '';
        showLogin();
    } catch (error) {
        showError(errorEl, error.message);
    } finally {
        btn.disabled = false;
        btnText.style.display = 'block';
        btnLoader.style.display = 'none';
    }
}

async function handleLogin(e) {
    e.preventDefault();

    const username = document.getElementById('login-username').value.trim();
    const password = document.getElementById('login-password').value;
    const errorEl = document.getElementById('login-error');
    const btn = e.target.querySelector('button');
    const btnText = btn.querySelector('.btn-text');
    const btnLoader = btn.querySelector('.btn-loader');

    if (!username || !password) {
        showError(errorEl, 'Kullanıcı adı ve şifre gereklidir.');
        return;
    }

    // Loading durumu
    btn.disabled = true;
    btnText.style.display = 'none';
    btnLoader.style.display = 'block';
    errorEl.classList.remove('show');

    try {
        await api.login(username, password);
        showToast('Giriş başarılı! 🎉', 'success');
        showMainApp();
    } catch (error) {
        showError(errorEl, error.message);
        btn.disabled = false;
        btnText.style.display = 'block';
        btnLoader.style.display = 'none';
    }
}

function showError(el, message) {
    el.textContent = message;
    el.classList.add('show');
}

function logout() {
    Auth.logout();
    showLogin();
    document.getElementById('main-app').style.display = 'none';
    showToast('Çıkış yapıldı', 'info');
}

// ============================================
// ANA UYGULAMA
// ============================================

async function showMainApp() {
    document.getElementById('login-page').style.display = 'none';
    document.getElementById('main-app').style.display = 'flex';

    const user = Auth.getUser();
    if (user) {
        // Header avatar
        document.getElementById('header-avatar').textContent = Utils.getInitials(user.name);
        document.getElementById('welcome-name').textContent = user.name;

        // Admin için alt navigasyon etiketini değiştir
        const navLoansLabel = document.getElementById('nav-loans-label');
        if (navLoansLabel) {
            navLoansLabel.textContent = user.role === 'Yonetici' ? 'Ödünç İşlemleri' : 'Ödünçlerim';
        }
    }

    // Verileri yükle
    await loadData();
    navigateTo('home');
}

async function loadData() {
    try {
        // Paralel yükleme
        const [kitaplar, turler, odunc, stats] = await Promise.all([
            api.getKitaplar(),
            api.getTurler(),
            api.getOdunclerim(),
            api.getIstatistikler()
        ]);

        allKitaplar = kitaplar || [];
        allTurler = turler || [];
        allOdunc = odunc || [];

        // İstatistikleri güncelle
        document.getElementById('stat-books').textContent = allKitaplar.length;
        document.getElementById('stat-loans').textContent = allOdunc.filter(o => !o.iadeTarihi).length;

        // Filtre chiplerini oluştur
        setupFilterChips();

        // Son kitapları göster
        renderRecentBooks();

    } catch (error) {
        console.error('Veri yükleme hatası:', error);
        showToast('Veriler yüklenirken hata oluştu', 'error');
    }
}

// ============================================
// NAVİGASYON
// ============================================

function navigateTo(page) {
    currentPage = page;

    // Sekmeleri güncelle
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.toggle('active', item.dataset.page === page);
    });

    // Sayfaları güncelle
    document.querySelectorAll('.section').forEach(section => {
        section.classList.remove('active');
    });
    document.getElementById(`${page}-section`).classList.add('active');

    // Sayfa başlığını güncelle
    const user = Auth.getUser();
    const isAdmin = user && user.role === 'Yonetici';

    const titles = {
        home: 'Ana Sayfa',
        books: 'Kitaplar',
        loans: isAdmin ? 'Ödünç İşlemleri' : 'Ödünçlerim',
        profile: 'Profil'
    };
    document.getElementById('page-title').textContent = titles[page] || 'Kütüphane';

    // Sayfa yükleme
    if (page === 'books') {
        renderBooks(allKitaplar);
    } else if (page === 'loans') {
        loadLoans();
    } else if (page === 'profile') {
        loadProfile();
    }

    // Sayfayı yukarı kaydır
    document.querySelector('.page-container').scrollTop = 0;
}

// ============================================
// ANA SAYFA
// ============================================

function renderRecentBooks() {
    const container = document.getElementById('recent-books');
    const recentBooks = allKitaplar.slice(0, 10);

    if (recentBooks.length === 0) {
        container.innerHTML = '<div class="loading-placeholder">Henüz kitap eklenmemiş</div>';
        return;
    }

    container.innerHTML = recentBooks.map(book => `
        <div class="book-card-mini" onclick="openBookModal(${book.kitapID})">
            <div class="book-title">${book.baslik || 'İsimsiz'}</div>
            <div class="book-author">✍️ ${book.yazar || 'Bilinmiyor'}</div>
            <span class="book-type">${book.turAdi || 'Genel'}</span>
        </div>
    `).join('');
}

// ============================================
// KİTAPLAR
// ============================================

function setupFilterChips() {
    const container = document.getElementById('filter-chips');
    const chips = ['Tümü', ...allTurler.map(t => t.turAdi)];

    container.innerHTML = chips.map((chip, i) => `
        <button class="chip ${i === 0 ? 'active' : ''}" data-filter="${i === 0 ? 'all' : chip}">${chip}</button>
    `).join('');

    container.querySelectorAll('.chip').forEach(chip => {
        chip.addEventListener('click', () => {
            container.querySelectorAll('.chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            filterBooks();
        });
    });
}

function filterBooks() {
    const searchText = document.getElementById('search-input').value.toLowerCase();
    const activeFilter = document.querySelector('.chip.active')?.dataset.filter || 'all';

    let filtered = allKitaplar;

    // Arama filtresi
    if (searchText) {
        filtered = filtered.filter(book =>
            (book.baslik || '').toLowerCase().includes(searchText) ||
            (book.yazar || '').toLowerCase().includes(searchText) ||
            (book.isbn || '').toLowerCase().includes(searchText)
        );
    }

    // Tür filtresi
    if (activeFilter !== 'all') {
        filtered = filtered.filter(book => book.turAdi === activeFilter);
    }

    renderBooks(filtered);
}

function renderBooks(books) {
    const container = document.getElementById('books-list');

    if (!books || books.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">📚</div>
                <h3>Kitap bulunamadı</h3>
                <p>Arama kriterlerinize uygun kitap yok</p>
            </div>
        `;
        return;
    }

    container.innerHTML = books.map(book => `
        <div class="book-card" onclick="openBookModal(${book.kitapID})">
            <div class="book-cover">📖</div>
            <div class="book-info">
                <div class="book-title">${book.baslik || 'İsimsiz'}</div>
                <div class="book-author">✍️ ${book.yazar || 'Bilinmiyor'}</div>
                <div class="book-meta">
                    <span class="badge badge-type">${book.turAdi || 'Genel'}</span>
                    ${book.mevcutAdet > 0
            ? `<span class="badge badge-available">✓ ${book.mevcutAdet} adet</span>`
            : '<span class="badge badge-unavailable">✗ Stokta yok</span>'
        }
                </div>
            </div>
        </div>
    `).join('');
}

// ============================================
// KİTAP DETAY MODAL
// ============================================

async function openBookModal(bookId) {
    const modal = document.getElementById('book-modal');
    const modalBody = document.getElementById('modal-body');
    const modalTitle = document.getElementById('modal-book-title');

    modal.classList.add('show');

    // Kitap bilgisini bul
    const book = allKitaplar.find(b => b.kitapID === bookId);
    modalTitle.textContent = book?.baslik || 'Kitap Detayı';

    // Loading
    modalBody.innerHTML = `
        <div class="loading-state">
            <div class="spinner"></div>
            <p>Yükleniyor...</p>
        </div>
    `;

    try {
        const [puan, degerlendirmeler] = await Promise.all([
            api.getKitapPuan(bookId),
            api.getKitapDegerlendirmeleri(bookId)
        ]);

        const ortalama = puan?.ortalamaPuan || 0;
        const sayisi = puan?.degerlendirmeSayisi || 0;
        const user = Auth.getUser();

        modalBody.innerHTML = `
            <!-- Puan Özeti -->
            <div class="rating-summary">
                <div class="rating-big">${ortalama.toFixed(1)}</div>
                <div class="rating-stars">${'⭐'.repeat(Math.round(ortalama))}</div>
                <div class="rating-count">${sayisi} değerlendirme</div>
            </div>

            <!-- Değerlendirme Formu -->
            <div class="rating-form">
                <h4>📝 Değerlendirmeniz</h4>
                <div class="star-rating" id="star-rating">
                    ${[1, 2, 3, 4, 5].map(i => `<span class="star" data-rating="${i}">☆</span>`).join('')}
                </div>
                <textarea id="review-text" placeholder="Yorumunuz (isteğe bağlı)..." rows="3"></textarea>
                <button class="btn btn-primary btn-block" onclick="submitReview(${bookId})">
                    Değerlendirmeyi Gönder
                </button>
            </div>

            <!-- Yorumlar -->
            <div class="reviews-section">
                <h4>💬 Yorumlar</h4>
                ${degerlendirmeler.length === 0
                ? '<p class="empty-state">Henüz yorum yok</p>'
                : degerlendirmeler.map(d => {
                    // Hem camelCase hem PascalCase alan isimlerini destekle
                    const degId = d.degerlendirmeID || d.DegerlendirmeID;
                    const uyeId = d.uyeID || d.UyeID;
                    const isOwner = user && (user.id == uyeId || String(user.id) === String(uyeId));
                    const isAdmin = user && user.role === 'Yonetici';
                    const canDelete = isOwner || isAdmin;

                    return `
                        <div class="review-card">
                            <div class="review-header">
                                <span class="review-author">👤 ${d.adSoyad || d.AdSoyad || 'Anonim'}</span>
                                <span class="review-stars">${'⭐'.repeat(d.puan || d.Puan || 0)}</span>
                                ${canDelete ? `<button class="delete-review-btn" onclick="deleteReview(${degId}, ${bookId})" style="background: #ff4757; border: none; color: white; padding: 4px 8px; border-radius: 4px; cursor: pointer; font-size: 12px;">🗑️</button>` : ''}
                            </div>
                            <div class="review-date">${Utils.formatDate(d.tarih || d.Tarih)}</div>
                            ${(d.yorum || d.Yorum) ? `<p class="review-text">${d.yorum || d.Yorum}</p>` : ''}
                        </div>
                    `;
                }).join('')
            }
            </div>
        `;

        // Yıldız seçimi
        setupStarRating();

    } catch (error) {
        modalBody.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">⚠️</div>
                <h3>Hata oluştu</h3>
                <p>${error.message}</p>
            </div>
        `;
    }
}

function setupStarRating() {
    selectedRating = 0;
    const stars = document.querySelectorAll('#star-rating .star');

    stars.forEach(star => {
        star.addEventListener('click', () => {
            selectedRating = parseInt(star.dataset.rating);
            updateStars(stars, selectedRating);
        });

        star.addEventListener('mouseover', () => {
            updateStars(stars, parseInt(star.dataset.rating));
        });

        star.addEventListener('mouseout', () => {
            updateStars(stars, selectedRating);
        });
    });
}

function updateStars(stars, rating) {
    stars.forEach((star, i) => {
        star.textContent = i < rating ? '★' : '☆';
        star.classList.toggle('active', i < rating);
    });
}

async function submitReview(bookId) {
    if (selectedRating < 1 || selectedRating > 5) {
        showToast('Lütfen 1-5 arası puan seçin', 'error');
        return;
    }

    const yorum = document.getElementById('review-text').value.trim();

    try {
        await api.degerlendirmeEkle(bookId, selectedRating, yorum);
        showToast('Değerlendirmeniz kaydedildi! 🎉', 'success');
        closeModal();
    } catch (error) {
        showToast('Hata: ' + error.message, 'error');
    }
}

async function deleteReview(reviewId, bookId) {
    console.log('deleteReview çağrıldı:', { reviewId, bookId });

    // Confirm'ı devre dışı bıraktık çünkü mobilde sorun çıkarabiliyor
    // if (!confirm('Bu yorumu silmek istediğinize emin misiniz?')) return;

    try {
        showToast('Yorum siliniyor...', 'info');
        const result = await api.degerlendirmeSil(reviewId);
        console.log('Silme sonucu:', result);
        showToast('Yorum başarıyla silindi!', 'success');
        openBookModal(bookId); // Yeniden yükle
    } catch (error) {
        console.error('Silme hatası:', error);
        showToast('Hata: ' + error.message, 'error');
    }
}

function closeModal() {
    document.getElementById('book-modal').classList.remove('show');
}

// ============================================
// ÖDÜNÇ İŞLEMLERİ
// ============================================

async function loadLoans() {
    const container = document.getElementById('loans-list');

    container.innerHTML = `
        <div class="loading-state">
            <div class="spinner"></div>
            <p>Yükleniyor...</p>
        </div>
    `;

    try {
        allOdunc = await api.getOdunclerim();
        const activeTab = document.querySelector('.loans-tabs .tab-btn.active').dataset.tab;
        renderLoans(activeTab);
    } catch (error) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">⚠️</div>
                <h3>Hata oluştu</h3>
                <p>${error.message}</p>
            </div>
        `;
    }
}

function renderLoans(tab) {
    const container = document.getElementById('loans-list');
    const user = Auth.getUser();
    const isAdmin = user && user.role === 'Yonetici';

    let filtered = allOdunc;

    if (tab === 'active') {
        filtered = allOdunc.filter(o => !o.iadeTarihi);
    } else {
        filtered = allOdunc.filter(o => o.iadeTarihi);
    }

    if (filtered.length === 0) {
        const emptyMsg = isAdmin
            ? (tab === 'active' ? 'Aktif ödünç işlemi yok' : 'Geçmiş ödünç işlemi yok')
            : (tab === 'active' ? 'Aktif ödünç işleminiz yok' : 'Geçmiş ödünç işleminiz yok');
        const emptyDesc = isAdmin
            ? (tab === 'active' ? 'Henüz hiçbir üyeye kitap verilmemiş' : 'İade edilmiş kitap bulunmuyor')
            : (tab === 'active' ? 'Kitap ödünç almak için kütüphaneyi ziyaret edin' : 'Henüz tamamlanmış ödünç işleminiz bulunmuyor');

        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">${tab === 'active' ? '📚' : '📋'}</div>
                <h3>${emptyMsg}</h3>
                <p>${emptyDesc}</p>
            </div>
        `;
        return;
    }

    container.innerHTML = filtered.map(loan => {
        const isOverdue = !loan.iadeTarihi && Utils.isOverdue(loan.iadeTarih || loan.beklenenIadeTarihi);
        const daysLeft = Utils.daysRemaining(loan.iadeTarih || loan.beklenenIadeTarihi);

        let statusClass = 'active';
        let statusText = `${daysLeft} gün kaldı`;

        if (loan.iadeTarihi) {
            statusClass = 'returned';
            statusText = 'İade edildi';
        } else if (isOverdue) {
            statusClass = 'overdue';
            statusText = `${Math.abs(daysLeft)} gün gecikme!`;
        }

        // Üye adını al (admin için gösterilecek)
        const uyeAdi = loan.uyeAdi || loan.adSoyad || '';

        return `
            <div class="loan-card">
                <div class="loan-book-title">📖 ${loan.kitapAdi || loan.baslik || 'Bilinmeyen Kitap'}</div>
                ${isAdmin && uyeAdi ? `<div class="loan-member" style="color: var(--primary); font-size: 0.85rem; margin-bottom: 0.5rem;">👤 ${uyeAdi}</div>` : ''}
                <div class="loan-dates">
                    <div class="loan-date-item">
                        <span class="loan-date-label">Alış Tarihi</span>
                        <span>${Utils.formatDate(loan.oduncTarihi)}</span>
                    </div>
                    <div class="loan-date-item">
                        <span class="loan-date-label">Son İade</span>
                        <span>${Utils.formatDate(loan.iadeTarih || loan.beklenenIadeTarihi)}</span>
                    </div>
                    ${loan.iadeTarihi ? `
                        <div class="loan-date-item">
                            <span class="loan-date-label">İade Tarihi</span>
                            <span>${Utils.formatDate(loan.iadeTarihi)}</span>
                        </div>
                    ` : ''}
                </div>
                <div class="loan-status ${statusClass}">${statusText}</div>
            </div>
        `;
    }).join('');
}

// ============================================
// PROFİL
// ============================================

async function loadProfile() {
    const user = Auth.getUser();

    // Temel bilgileri hemen göster
    document.getElementById('profile-avatar').textContent = Utils.getInitials(user?.name);
    document.getElementById('profile-name').textContent = user?.name || 'Kullanıcı';
    document.getElementById('profile-role').textContent = user?.role === 'Yonetici' ? '👑 Yönetici' : '👤 Üye';

    try {
        const profil = await api.getProfilBilgileri();
        console.log('Profil verisi:', profil);

        if (profil) {
            // Hem camelCase hem PascalCase alan isimlerini destekle
            const getValue = (obj, ...keys) => {
                for (const key of keys) {
                    if (obj[key] !== undefined && obj[key] !== null) {
                        return typeof obj[key] === 'object' ? JSON.stringify(obj[key]) : String(obj[key]);
                    }
                }
                return '-';
            };

            document.getElementById('profile-fullname').textContent = getValue(profil, 'adSoyad', 'AdSoyad');
            document.getElementById('profile-email').textContent = getValue(profil, 'email', 'Email');
            document.getElementById('profile-phone').textContent = getValue(profil, 'telefon', 'Telefon');
            document.getElementById('profile-date').textContent = Utils.formatDate(
                profil.kayitTarihi || profil.KayitTarihi || profil.olusturmaTarihi || profil.OlusturmaTarihi
            );
        }
    } catch (error) {
        console.error('Profil yükleme hatası:', error);
    }
}

// ============================================
// PWA SERVICE WORKER
// ============================================

if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('./sw.js')
            .then(reg => console.log('SW registered'))
            .catch(err => console.log('SW registration failed:', err));
    });
}
