import { api, ApiError } from "../js/api.js";

const charts = {};
let currentUser = null;

const moduleInfo = {
    usuarios: {
        icon: "fa-users-gear",
        title: "Usuarios",
        text: "Base lista para gestionar usuarios, estados, roles y permisos con las APIs de Seguridad.",
        endpoints: ["/api/security/users", "/api/security/roles", "/api/security/permissions"]
    },
    clientes: {
        icon: "fa-address-card",
        title: "Clientes",
        text: "Base lista para consultar clientes, estados y su relación con usuarios autenticados.",
        endpoints: ["/api/clientes"]
    },
    catalogo: {
        icon: "fa-map-location-dot",
        title: "Catálogo turístico",
        text: "Base lista para administrar destinos, paquetes y tarifas usando permisos de catálogo.",
        endpoints: ["/api/catalogo/destinos", "/api/catalogo/paquetes", "/api/catalogo/opciones"]
    },
    operacion: {
        icon: "fa-route",
        title: "Operación",
        text: "Base lista para gestionar salidas, cupos, proveedores, recursos, bloqueos e itinerarios.",
        endpoints: ["/api/operacion/salidas", "/api/operacion/proveedores", "/api/operacion/recursos"]
    },
    reservas: {
        icon: "fa-calendar-check",
        title: "Reservas",
        text: "Base lista para seguimiento administrativo de reservas, pasajeros, estados e historial.",
        endpoints: ["/api/reservas", "/api/reservas/{id}", "/api/reservas/{id}/estado"]
    },
    pagos: {
        icon: "fa-credit-card",
        title: "Pagos",
        text: "Base lista para revisar órdenes, pagos y estados de transacciones.",
        endpoints: ["/api/pagos/admin/ordenes", "/api/pagos/admin/pagos"]
    },
    ventas: {
        icon: "fa-receipt",
        title: "Ventas",
        text: "Base lista para compras, comprobantes, tickets, vouchers y moderación de reseñas.",
        endpoints: ["/api/ventas/admin/compras", "/api/ventas/admin/resenas"]
    },
    auditoria: {
        icon: "fa-shield-halved",
        title: "Auditoría",
        text: "Base lista para consultar accesos, cambios, errores y actividad reciente.",
        endpoints: ["/api/auditoria/accesos", "/api/auditoria/cambios", "/api/auditoria/errores"]
    }
};

document.addEventListener("DOMContentLoaded", init);

async function init() {
    bindLayout();
    renderPlaceholders();
    try {
        currentUser = await api("/api/auth/me");
        renderUser(currentUser);
        applyPermissions(currentUser);
        await Promise.all([loadDashboard(), loadReports()]);
    } catch (error) {
        handleAccessError(error);
    }
}

function bindLayout() {
    document.getElementById("adminMenuToggle")?.addEventListener("click", () => {
        document.getElementById("adminSidebar")?.classList.toggle("open");
    });

    document.querySelectorAll(".admin-nav a").forEach(link => {
        link.addEventListener("click", event => {
            const href = link.getAttribute("href") || "";
            if (!href.startsWith("#")) return;
            event.preventDefault();
            activateSection(href.replace("#", "") || "dashboard");
            document.getElementById("adminSidebar")?.classList.remove("open");
        });
    });

    document.getElementById("refreshDashboard")?.addEventListener("click", () => {
        loadDashboard();
        loadReports();
    });

    window.addEventListener("hashchange", () => {
        const id = location.hash.replace("#", "");
        if (id) activateSection(id);
    });
}

function activateSection(id) {
    const target = document.getElementById(id);
    if (!target) return;
    document.querySelectorAll(".admin-section").forEach(section => section.classList.toggle("active", section.id === id));
    document.querySelectorAll(".admin-nav a").forEach(link => link.classList.toggle("active", link.getAttribute("href") === `#${id}`));
    document.getElementById("adminTitle").textContent = target.dataset.title || "Panel Administrativo";
}

function renderUser(user) {
    document.getElementById("adminUserName").textContent = user.nombre || user.email || "Usuario WayPath";
    document.getElementById("adminUserRole").textContent = primaryRole(user);
}

function primaryRole(user) {
    const roles = user?.roles || [];
    if (roles.includes("ADMINISTRADOR")) return "Administrador";
    if (roles.includes("EMPLEADO")) return "Empleado";
    if (roles.includes("CLIENTE")) return "Cliente";
    return roles[0] || "Sin rol";
}

function applyPermissions(user) {
    const permissions = new Set(user?.permisos || []);
    const isAdmin = (user?.roles || []).includes("ADMINISTRADOR");
    const canUseAdmin = isAdmin || permissions.has("SEGURIDAD_USUARIOS_GESTIONAR") || permissions.has("REPORTES_LEER") || permissions.has("AUDITORIA_LEER");

    if (!canUseAdmin) {
        showAccessDenied("Tu usuario no tiene acceso administrativo.");
        return;
    }

    document.querySelectorAll("[data-permission]").forEach(item => {
        const permission = item.dataset.permission;
        item.hidden = !(isAdmin || permissions.has(permission));
    });

    const firstVisible = document.querySelector(".admin-nav a:not([hidden])");
    const requested = location.hash.replace("#", "");
    const requestedLink = requested ? document.querySelector(`.admin-nav a[href="#${CSS.escape(requested)}"]:not([hidden])`) : null;
    if (requestedLink) activateSection(requested);
    else if (firstVisible) {
        const href = firstVisible.getAttribute("href") || "#dashboard";
        if (href.startsWith("#")) activateSection(href.replace("#", ""));
        else window.location.href = href;
    }
}

function renderPlaceholders() {
    Object.entries(moduleInfo).forEach(([id, info]) => {
        const host = document.querySelector(`#${id} .admin-placeholder`);
        if (!host) return;
        host.innerHTML = `
            <div class="admin-placeholder-inner">
                <i class="fa-solid ${info.icon}" aria-hidden="true"></i>
                <h2>${escapeHtml(info.title)}</h2>
                <p>${escapeHtml(info.text)}</p>
                <div class="admin-module-endpoints">
                    ${info.endpoints.map(endpoint => `<code>${escapeHtml(endpoint)}</code>`).join("")}
                </div>
            </div>`;
    });
}

async function loadDashboard() {
    try {
        const [summary, commercial, reservations, payments, sales, operation] = await Promise.all([
            api("/api/admin/dashboard/resumen"),
            api("/api/admin/dashboard/metricas-comerciales"),
            api("/api/admin/dashboard/reservas"),
            api("/api/admin/dashboard/pagos"),
            api("/api/admin/dashboard/ventas"),
            api("/api/admin/dashboard/operacion")
        ]);
        renderSummary(summary);
        renderDashboardCharts(commercial, reservations, payments, sales, operation);
        status("dashboardStatus", "Métricas actualizadas desde WayPathDB.");
    } catch (error) {
        const message = error instanceof ApiError && error.status === 403
            ? "Tu usuario no tiene permiso REPORTES_LEER."
            : error.message || "No se pudieron cargar las métricas administrativas.";
        status("dashboardStatus", message);
        renderSummary({});
        renderDashboardCharts(emptyCommercial(), { porEstado: [] }, { porMetodo: [] }, emptySales(), {});
    }
}

async function loadReports() {
    try {
        const [clientes, eventos, resenas, operacion] = await Promise.all([
            api("/api/reportes/clientes"),
            api("/api/reportes/eventos/resumen"),
            api("/api/reportes/resenas"),
            api("/api/reportes/operacion")
        ]);
        chartBar("graficoCampanas", labels(eventos), values(eventos), "Eventos registrados", true);
        chartBar("graficoSocial", labels(clientes.nuevosPorMes), values(clientes.nuevosPorMes), "Clientes nuevos", false);
        chartPolar("graficoEdades", labels(resenas), values(resenas));
        chartDoughnut("graficoDispositivos", labels(operacion.salidasPorEstado), values(operacion.salidasPorEstado));
        status("reportesStatus", "Reportes cargados desde WayPathDB.");
    } catch (error) {
        status("reportesStatus", error.message || "No se pudieron cargar los reportes.");
        chartBar("graficoCampanas", ["Sin datos"], [0], "Eventos registrados", true);
        chartBar("graficoSocial", ["Sin datos"], [0], "Clientes nuevos", false);
        chartPolar("graficoEdades", ["Sin datos"], [0]);
        chartDoughnut("graficoDispositivos", ["Sin datos"], [0]);
    }
}

function renderSummary(data) {
    text("kpiUsuarios", number(data.usuariosRegistrados));
    text("kpiClientes", number(data.clientesActivos));
    text("kpiReservas", number(data.reservasTotales));
    text("kpiVentas", money(data.totalVendido));
    text("kpiCompras", number(data.comprasGeneradas));
    text("kpiCupos", number(data.cuposDisponibles));
}

function renderDashboardCharts(commercial, reservations, payments, sales) {
    chartBar("graficoConversion", ["Usuarios", "Clientes", "Reservas", "Compras"], [
        numericText("kpiUsuarios"),
        numericText("kpiClientes"),
        sum(reservations.porEstado),
        sum(sales.paquetesMasVendidos)
    ], "Cantidad", false);
    chartLine("graficoIngresos", labels(sales.ingresosMensuales), values(sales.ingresosMensuales));
    chartDoughnut("graficoDestinos", labels(commercial.destinosMasVendidos), values(commercial.destinosMasVendidos));
    chartPie("graficoPagos", labels(payments.porMetodo), values(payments.porMetodo));
}

function chartBar(id, chartLabels, data, label, horizontal) {
    createChart(id, {
        type: "bar",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ label, data: fallbackValues(data), backgroundColor: ["#2563eb", "#f97316", "#15803d", "#7c3aed", "#dc2626"] }]
        },
        options: { ...chartOptions(), indexAxis: horizontal ? "y" : "x" }
    });
}

function chartLine(id, chartLabels, data) {
    createChart(id, {
        type: "line",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{
                label: "Ingresos (S/)",
                data: fallbackValues(data),
                borderColor: "#f97316",
                backgroundColor: "rgba(249,115,22,.16)",
                fill: true,
                tension: .32
            }]
        },
        options: chartOptions()
    });
}

function chartDoughnut(id, chartLabels, data) {
    createChart(id, {
        type: "doughnut",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ data: fallbackValues(data), backgroundColor: ["#2563eb", "#f97316", "#15803d", "#7c3aed", "#14b8a6"] }]
        },
        options: chartOptions(false)
    });
}

function chartPie(id, chartLabels, data) {
    createChart(id, {
        type: "pie",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ data: fallbackValues(data), backgroundColor: ["#003a7a", "#f97316", "#64748b", "#15803d"] }]
        },
        options: chartOptions(false)
    });
}

function chartPolar(id, chartLabels, data) {
    createChart(id, {
        type: "polarArea",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ data: fallbackValues(data), backgroundColor: ["#ff6384b3", "#36a2ebb3", "#ffce56b3", "#4bc0c0b3", "#9966ffb3"] }]
        },
        options: chartOptions(false)
    });
}

function createChart(id, config) {
    const canvas = document.getElementById(id);
    if (!canvas || !window.Chart) return;
    charts[id]?.destroy();
    charts[id] = new Chart(canvas.getContext("2d"), config);
}

function chartOptions(withScales = true) {
    const color = getComputedStyle(document.documentElement).getPropertyValue("--text-muted").trim() || "#64748b";
    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { labels: { color, boxWidth: 14 } } }
    };
    if (withScales) {
        options.scales = {
            x: { ticks: { color }, grid: { color: "rgba(148,163,184,.18)" } },
            y: { beginAtZero: true, ticks: { color, precision: 0 }, grid: { color: "rgba(148,163,184,.18)" } }
        };
    }
    return options;
}

function handleAccessError(error) {
    if (error instanceof ApiError && error.status === 401) {
        showAccessDenied("Inicia sesión como administrador o empleado autorizado para ingresar al panel.");
        return;
    }
    showAccessDenied(error.message || "No se pudo validar el acceso administrativo.");
}

function showAccessDenied(message) {
    document.getElementById("adminTitle").textContent = "Acceso administrativo";
    document.getElementById("adminUserName").textContent = "Sesión requerida";
    document.getElementById("adminUserRole").textContent = "Sin acceso";
    document.querySelector(".admin-main").innerHTML = `
        <section class="admin-placeholder">
            <div class="admin-placeholder-inner">
                <i class="fa-solid fa-lock" aria-hidden="true"></i>
                <h2>Acceso restringido</h2>
                <p>${escapeHtml(message)}</p>
                <a class="wp-btn wp-btn-primary" href="../login.html">Iniciar sesión</a>
            </div>
        </section>`;
}

function labels(rows) {
    return (rows || []).map(x => x.etiqueta || x.tipoEvento);
}

function values(rows) {
    return (rows || []).map(x => Number(x.valor ?? x.total ?? 0));
}

function fallbackLabels(rows) {
    return rows?.length ? rows : ["Sin datos"];
}

function fallbackValues(rows) {
    return rows?.length && rows.some(x => Number(x) > 0) ? rows : [0];
}

function sum(rows) {
    return values(rows).reduce((acc, item) => acc + item, 0);
}

function numericText(id) {
    return Number(document.getElementById(id)?.textContent.replace(/[^\d.-]/g, "") || 0);
}

function number(value) {
    return Number(value || 0).toLocaleString("es-PE");
}

function money(value) {
    return Number(value || 0).toLocaleString("es-PE", { style: "currency", currency: "PEN" });
}

function text(id, value) {
    const element = document.getElementById(id);
    if (element) element.textContent = value;
}

function status(id, value) {
    text(id, value);
}

function emptyCommercial() {
    return { destinosMasVendidos: [], paquetesMasVendidos: [] };
}

function emptySales() {
    return { ingresosMensuales: [], paquetesMasVendidos: [] };
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, char => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    })[char]);
}
