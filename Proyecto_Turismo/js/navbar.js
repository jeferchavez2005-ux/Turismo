(function () {
    const path = window.location.pathname.replace(/\\/g, "/");
    const page = path.split("/").pop() || "index.html";
    const section = path.includes("/dashboards/")
        ? "dashboards"
        : path.includes("/landings_pages/")
            ? "landings_pages"
            : "root";
    const prefix = section === "root" ? "" : "../";
    const currentRoute = section === "root" ? page : `${section}/${page}`;
    let currentUser = null;

    const publicLinks = [
        { href: "index.html", key: "nav.home", icon: "fa-house" },
        { href: "Destinos.html", key: "nav.destinations", icon: "fa-map-location-dot" },
        { href: "servicios.html", key: "nav.services", icon: "fa-concierge-bell" },
        { href: "contacto.html", key: "nav.contact", icon: "fa-envelope" }
    ];
    const clientLinks = [
        { href: "cuenta.html", key: "nav.account", icon: "fa-user" },
        { href: "reserva.html", key: "nav.reservations", icon: "fa-calendar-check" },
        { href: "compras.html", key: "nav.purchases", icon: "fa-bag-shopping" },
        { href: "reseñas.html", key: "nav.reviews", icon: "fa-star" }
    ];
    const adminLinks = [
        { href: "admin/index.html", key: "nav.dashboard", icon: "fa-table-columns" },
        { href: "dashboards/dashboard-marketing.html", key: "nav.reports", icon: "fa-chart-pie" }
    ];

    function t(key) {
        return window.WayPathI18n?.t(key) || fallback[key] || key;
    }

    const fallback = {
        "nav.home": "Inicio",
        "nav.destinations": "Destinos",
        "nav.services": "Servicios",
        "nav.contact": "Contacto",
        "nav.login": "Iniciar sesion",
        "nav.register": "Registrarse",
        "nav.account": "Mi cuenta",
        "nav.reservations": "Reservas",
        "nav.purchases": "Mis compras",
        "nav.reviews": "Resenas",
        "nav.dashboard": "Dashboard",
        "nav.reports": "Reportes",
        "nav.landings": "Landing Pages",
        "footer.tagline": "Tu agencia de viajes digital para explorar el Peru y el mundo con confianza.",
        "footer.pages": "Paginas",
        "footer.legal": "Legal",
        "footer.privacy": "Privacidad",
        "footer.terms": "Terminos",
        "footer.support": "Soporte",
        "footer.rights": "Todos los derechos reservados."
    };

    function href(target) {
        if (target.startsWith("dashboards/") && section === "dashboards") {
            return target.replace("dashboards/", "");
        }
        if (target.startsWith("landings_pages/") && section === "landings_pages") {
            return target.replace("landings_pages/", "");
        }
        return `${prefix}${target}`;
    }

    function isActive(target) {
        return target === currentRoute;
    }

    function linkTemplate(link, mobile = false) {
        const active = isActive(link.href) ? " active" : "";
        const icon = mobile ? `<i class="fa-solid ${link.icon}" aria-hidden="true"></i>` : "";
        const extraClass = link.className ? ` ${link.className}` : "";
        return `<a href="${href(link.href)}" class="nav-link${active}${extraClass}" data-i18n="${link.key}">${icon}${t(link.key)}</a>`;
    }

    function hasRole(user, role) {
        return Array.isArray(user?.roles) && user.roles.some(value => value.toUpperCase() === role);
    }

    function canSeeAdmin(user) {
        if (hasRole(user, "ADMINISTRADOR") || hasRole(user, "EMPLEADO")) return true;
        return Array.isArray(user?.permisos) && user.permisos.includes("REPORTES_LEER");
    }

    function buildLinks(user) {
        if (!user) {
            return {
                desktop: [
                    ...publicLinks,
                    { href: "registro.html", key: "nav.register", icon: "fa-user-plus" }
                ],
                mobile: [
                    ...publicLinks,
                    { href: "login.html", key: "nav.login", icon: "fa-right-to-bracket", className: "mobile-login login-btn" },
                    { href: "registro.html", key: "nav.register", icon: "fa-user-plus" }
                ]
            };
        }

        const desktop = [...publicLinks, ...clientLinks.filter(link => link.key !== "nav.account")];
        const mobile = [...publicLinks, ...clientLinks];
        if (canSeeAdmin(user)) {
            desktop.push(...adminLinks);
            mobile.push(...adminLinks);
        }
        return { desktop, mobile };
    }

    function renderNav(user) {
        const desktopNav = document.querySelector(".wp-nav-links");
        const mobileNav = document.getElementById("mobileMenu");
        if (!desktopNav && !mobileNav) return;

        if (desktopNav) desktopNav.setAttribute("aria-label", "Menu principal");
        if (mobileNav) mobileNav.setAttribute("aria-label", "Menu movil");

        const links = buildLinks(user);
        if (desktopNav) desktopNav.innerHTML = links.desktop.map(link => linkTemplate(link)).join("");
        if (mobileNav) mobileNav.innerHTML = links.mobile.map(link => linkTemplate(link, true)).join("");

        const loginTargets = document.querySelectorAll(".login-btn");
        loginTargets.forEach(button => {
            const isAuthenticated = Boolean(user);
            button.href = isAuthenticated ? href("cuenta.html") : href("login.html");
            button.title = user?.nombre || user?.email || t("nav.login");
            button.innerHTML = `<i class="fa-solid ${isAuthenticated ? "fa-user" : "fa-right-to-bracket"}" aria-hidden="true"></i> ${isAuthenticated ? t("nav.account") : t("nav.login")}`;
        });

        window.WayPathI18n?.applyTranslations();
    }

    function renderFooter() {
        const footer = document.querySelector(".wp-footer");
        if (!footer) return;
        footer.setAttribute("role", "contentinfo");
        footer.innerHTML = `
            <div class="wp-footer-inner">
                <div>
                    <span class="wp-footer-logo">Way<span class="accent">Path</span></span>
                    <p class="wp-footer-tagline" data-i18n="footer.tagline">${t("footer.tagline")}</p>
                </div>
                <div class="wp-footer-col">
                    <h4 data-i18n="footer.pages">${t("footer.pages")}</h4>
                    ${publicLinks.map(link => `<a href="${href(link.href)}" data-i18n="${link.key}">${t(link.key)}</a>`).join("")}
                </div>
                <div class="wp-footer-col">
                    <h4 data-i18n="footer.support">${t("footer.support")}</h4>
                    <a href="${href("contacto.html")}" data-i18n="footer.support">${t("footer.support")}</a>
                </div>
            </div>
            <div class="wp-footer-bottom">
                <span>© 2026 WayPath. <span data-i18n="footer.rights">${t("footer.rights")}</span></span>
            </div>`;
    }

    function bindHamburger() {
        const hamburger = document.getElementById("hamburger");
        const mobileMenu = document.getElementById("mobileMenu");
        if (!hamburger || !mobileMenu || hamburger.dataset.wpBound === "true") return;
        hamburger.dataset.wpBound = "true";

        const updateMenuPosition = () => {
            if (!mobileMenu.classList.contains("open")) return;
            const navbar = document.querySelector(".wp-navbar");
            const top = Math.max(0, navbar?.getBoundingClientRect().bottom || 0);
            mobileMenu.style.top = `${top}px`;
            mobileMenu.style.maxHeight = `${Math.max(180, window.innerHeight - top)}px`;
        };

        const setMenuOpen = (isOpen, returnFocus = false) => {
            hamburger.classList.toggle("open", isOpen);
            mobileMenu.classList.toggle("open", isOpen);
            hamburger.setAttribute("aria-expanded", String(isOpen));
            hamburger.setAttribute("aria-label", isOpen ? "Cerrar menu" : "Abrir menu");
            if (isOpen) {
                updateMenuPosition();
                mobileMenu.querySelector("a")?.focus();
            } else {
                mobileMenu.style.removeProperty("top");
                mobileMenu.style.removeProperty("max-height");
                if (returnFocus) hamburger.focus();
            }
        };

        hamburger.addEventListener("click", () => {
            const isOpen = !mobileMenu.classList.contains("open");
            setMenuOpen(isOpen);
        });
        mobileMenu.addEventListener("click", event => {
            if (event.target.closest("a")) setMenuOpen(false);
        });
        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && mobileMenu.classList.contains("open")) setMenuOpen(false, true);
        });
        window.addEventListener("resize", updateMenuPosition);
        window.addEventListener("scroll", updateMenuPosition, { passive: true });
    }

    async function getUser() {
        try {
            const module = await import("./api.js");
            return await module.api("/api/auth/me");
        } catch {
            return null;
        }
    }

    async function init() {
        bindHamburger();
        renderFooter();
        renderNav(null);
        currentUser = await getUser();
        renderNav(currentUser);
    }

    init();
    window.addEventListener("waypath:languagechange", () => {
        renderFooter();
        renderNav(currentUser);
    });
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) getUser().then(user => {
            currentUser = user;
            renderNav(currentUser);
        });
    });
})();
