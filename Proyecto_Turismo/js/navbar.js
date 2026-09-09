// navbar.js – Inyecta el navbar y footer en todas las páginas
// Incluir ANTES de cerrar </body>

(function() {
    // Detectar página activa
    const path = window.location.pathname.split('/').pop() || 'index.html';

    const links = [
        { href: 'servicios.html', label: 'Nuestros servicios', icon: 'fa-concierge-bell' },
        { href: 'Destinos.html',  label: 'Destinos',           icon: 'fa-map-location-dot' },
        { href: 'compras.html',   label: 'Mis compras',        icon: 'fa-bag-shopping' },
        { href: 'reseñas.html',   label: 'Nuestras reseñas',   icon: 'fa-star' },
        { href: 'contacto.html',  label: 'Contacto',           icon: 'fa-envelope' },
    ];

    function navLinkHTML(l, mobile = false) {
        const active = path === l.href ? ' active' : '';
        if (mobile) {
            return `<a href="${l.href}" class="nav-link${active}"><i class="fa-solid ${l.icon}"></i> ${l.label}</a>`;
        }
        return `<a href="${l.href}" class="nav-link${active}">${l.label}</a>`;
    }

    const navEl = document.querySelector('.wp-navbar');
    const mobileEl = document.getElementById('mobileMenu');

    // Hamburger
    const hamburger = document.getElementById('hamburger');
    if (hamburger && mobileEl) {
        hamburger.addEventListener('click', () => {
            hamburger.classList.toggle('open');
            mobileEl.classList.toggle('open');
            hamburger.setAttribute('aria-expanded', mobileEl.classList.contains('open'));
        });
    }

    // Login state
    const user = localStorage.getItem('waypath_usuario');
    if (user) {
        const name = user.split(' ')[0];
        document.querySelectorAll('.login-btn').forEach(btn => {
            btn.innerHTML = `<i class="fa-solid fa-right-from-bracket"></i> Salir (${name})`;
            btn.href = '#';
            btn.style.cssText = 'background:linear-gradient(135deg,#dc2626,#ef4444)!important;color:#fff!important;';
            btn.addEventListener('click', e => {
                e.preventDefault();
                localStorage.removeItem('waypath_usuario');
                window.location.reload();
            });
        });
    }
})();
