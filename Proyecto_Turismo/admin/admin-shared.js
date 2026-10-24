import { api, ApiError } from "../js/api.js";

export const adminLinks = [
    { href: "index.html#dashboard", label: "Dashboard", i18n: "admin.nav.dashboard", icon: "fa-chart-line", permission: "REPORTES_LEER" },
    { href: "usuarios.html", label: "Usuarios", i18n: "admin.nav.users", icon: "fa-users-gear", permission: "SEGURIDAD_USUARIOS_GESTIONAR" },
    { href: "clientes.html", label: "Clientes", i18n: "admin.nav.clients", icon: "fa-address-card", permission: "SEGURIDAD_USUARIOS_GESTIONAR" },
    { href: "catalogo.html", label: "Catálogo", i18n: "admin.nav.catalog", icon: "fa-map-location-dot", permission: "CATALOGO_DESTINOS_GESTIONAR" },
    { href: "operacion.html", label: "Operación", i18n: "admin.nav.operation", icon: "fa-route", permission: "VENTAS_RESERVAS_GESTIONAR" },
    { href: "reservas.html", label: "Reservas", i18n: "admin.nav.reservations", icon: "fa-calendar-check", permission: "VENTAS_RESERVAS_LEER" },
    { href: "pagos.html", label: "Pagos", i18n: "admin.nav.payments", icon: "fa-credit-card", permission: "PAGOS_LEER" },
    { href: "ventas.html", label: "Ventas", i18n: "admin.nav.sales", icon: "fa-receipt", permission: "VENTAS_RESERVAS_LEER" },
    { href: "index.html#reportes", label: "Reportes", i18n: "admin.nav.reports", icon: "fa-chart-pie", permission: "REPORTES_LEER" },
    { href: "auditoria.html", label: "Auditoría", i18n: "admin.nav.audit", icon: "fa-shield-halved", permission: "AUDITORIA_LEER" }
];

export async function initAdminPage({ active, title }) {
    bindMenu();
    enhanceAdminTables();
    const user = await requireAdminAccess();
    renderShell(user, active, title);
    return user;
}

export function can(user, permission) {
    return (user?.roles || []).includes("ADMINISTRADOR") || (user?.permisos || []).includes(permission);
}

export function stateBadge(value) {
    const normalized = String(value || "").toUpperCase();
    const type = ["ACTIVO", "APROBADO", "APROBADA", "CONFIRMADA", "COMPLETADA", "EMITIDA"].includes(normalized)
        ? "success"
        : ["PENDIENTE", "PENDIENTE_PAGO", "BORRADOR"].includes(normalized)
            ? "warning"
            : ["BLOQUEADO", "INACTIVO", "CANCELADA", "RECHAZADO", "RECHAZADA", "ANULADA"].includes(normalized)
                ? "danger"
                : "muted";
    return `<span class="wp-status wp-status-${type}">${escapeHtml(value || "Sin estado")}</span>`;
}

export function adminLoading(message = "Cargando datos...") {
    return `<div class="wp-empty-state"><span class="wp-loader"></span><strong>${escapeHtml(message)}</strong></div>`;
}

export function adminEmpty(title, text = "No hay registros para mostrar.") {
    return `<div class="wp-empty-state"><i class="fa-solid fa-circle-info" aria-hidden="true"></i><strong>${escapeHtml(title)}</strong><p>${escapeHtml(text)}</p></div>`;
}

export function adminError(message) {
    return `<div class="wp-alert wp-alert-danger" role="alert"><i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i><span>${escapeHtml(message)}</span></div>`;
}

export function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, char => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    })[char]);
}

function bindMenu() {
    const toggle = document.getElementById("adminMenuToggle");
    const sidebar = document.getElementById("adminSidebar");
    if (!toggle || !sidebar) return;

    const closeMenu = () => {
        sidebar.classList.remove("open");
        document.body.classList.remove("admin-menu-open");
        toggle.setAttribute("aria-expanded", "false");
    };

    toggle.setAttribute("aria-controls", "adminSidebar");
    toggle.setAttribute("aria-expanded", "false");
    toggle.addEventListener("click", () => {
        const isOpen = sidebar.classList.toggle("open");
        document.body.classList.toggle("admin-menu-open", isOpen);
        toggle.setAttribute("aria-expanded", String(isOpen));
    });
    sidebar.addEventListener("click", event => {
        if (event.target.closest("a")) closeMenu();
    });
    document.addEventListener("click", event => {
        if (sidebar.classList.contains("open") && !sidebar.contains(event.target) && !toggle.contains(event.target)) closeMenu();
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && sidebar.classList.contains("open")) {
            closeMenu();
            toggle.focus();
        }
    });
}

function enhanceAdminTables() {
    const annotate = root => {
        root.querySelectorAll?.(".wp-table").forEach(table => {
            const labels = [...table.querySelectorAll("thead th")].map(cell => cell.textContent.trim());
            table.querySelectorAll("tbody tr").forEach(row => {
                [...row.children].forEach((cell, index) => cell.dataset.label = labels[index] || "Dato");
            });
        });
    };
    annotate(document);
    new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => {
        if (node.nodeType === Node.ELEMENT_NODE) annotate(node);
    }))).observe(document.querySelector(".admin-main") || document.body, { childList: true, subtree: true });
}

async function requireAdminAccess() {
    try {
        const user = await api("/api/auth/me");
        const hasAdminAccess = (user.roles || []).includes("ADMINISTRADOR")
            || (user.roles || []).includes("EMPLEADO")
            || (user.permisos || []).some(permission => [
                "SEGURIDAD_USUARIOS_GESTIONAR",
                "CATALOGO_DESTINOS_GESTIONAR",
                "VENTAS_RESERVAS_LEER",
                "VENTAS_RESERVAS_GESTIONAR",
                "PAGOS_LEER",
                "PAGOS_GESTIONAR",
                "REPORTES_LEER",
                "AUDITORIA_LEER"
            ].includes(permission));
        if (!hasAdminAccess) throw new ApiError("Tu usuario no tiene acceso administrativo.", 403);
        return user;
    } catch (error) {
        renderAccessDenied(error);
        throw error;
    }
}

function renderShell(user, active, title) {
    document.getElementById("adminTitle").textContent = title || "Panel Administrativo";
    document.getElementById("adminUserName").textContent = user.nombre || user.email || "Usuario WayPath";
    document.getElementById("adminUserRole").textContent = primaryRole(user);

    const nav = document.getElementById("adminNav");
    if (nav) {
        nav.innerHTML = adminLinks
            .filter(link => can(user, link.permission))
            .map(link => `<a href="${link.href}" class="${active === link.label ? "active" : ""}"><i class="fa-solid ${link.icon}" aria-hidden="true"></i> <span data-i18n="${link.i18n}">${link.label}</span></a>`)
            .join("");
        window.WayPathI18n?.applyTranslations(nav);
    }
}

function primaryRole(user) {
    const roles = user?.roles || [];
    if (roles.includes("ADMINISTRADOR")) return "Administrador";
    if (roles.includes("EMPLEADO")) return "Empleado";
    if (roles.includes("CLIENTE")) return "Cliente";
    return roles[0] || "Sin rol";
}

function renderAccessDenied(error) {
    const message = error instanceof ApiError && error.status === 401
        ? "Inicia sesión como administrador o empleado autorizado."
        : error.message || "No se pudo validar el acceso administrativo.";
    document.getElementById("adminTitle").textContent = "Acceso administrativo";
    const main = document.querySelector(".admin-main");
    if (main) {
        main.innerHTML = `
            <section class="admin-placeholder">
                <div class="admin-placeholder-inner">
                    <i class="fa-solid fa-lock" aria-hidden="true"></i>
                    <h2>Acceso restringido</h2>
                    <p>${escapeHtml(message)}</p>
                    <a class="wp-btn wp-btn-primary" href="../login.html">Iniciar sesión</a>
                </div>
            </section>`;
    }
}
