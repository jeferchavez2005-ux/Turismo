import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = {
    activeTab: "accesos",
    page: 1,
    pageSize: 50,
    total: 0,
    summary: null,
    accesses: [],
    changes: [],
    errors: [],
    recent: null,
    user: null
};

document.addEventListener("DOMContentLoaded", init);

async function init() {
    try {
        state.user = await initAdminPage({ active: "Auditoría", title: "Auditoría" });
    } catch {
        return;
    }
    if (!can(state.user, "AUDITORIA_LEER")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.querySelectorAll(".admin-tab").forEach(button => {
        button.addEventListener("click", async () => {
            state.activeTab = button.dataset.tab || "accesos";
            state.page = 1;
            document.querySelectorAll(".admin-tab").forEach(tab => tab.classList.toggle("active", tab === button));
            await loadCurrentTab();
        });
    });
    document.getElementById("buscarAuditoria")?.addEventListener("input", render);
    document.getElementById("filtroAuditoria")?.addEventListener("change", () => { state.page = 1; loadCurrentTab(); });
    document.getElementById("filtroDesdeAuditoria")?.addEventListener("change", () => { state.page = 1; loadCurrentTab(); });
    document.getElementById("filtroHastaAuditoria")?.addEventListener("change", () => { state.page = 1; loadCurrentTab(); });
    document.getElementById("recargarAuditoria")?.addEventListener("click", loadData);
    document.getElementById("auditPrev")?.addEventListener("click", () => changePage(-1));
    document.getElementById("auditNext")?.addEventListener("click", () => changePage(1));
}

async function loadData() {
    const host = document.getElementById("auditoriaTableHost");
    host.innerHTML = adminLoading("Cargando auditoría...");
    try {
        const [summary, recent] = await Promise.all([
            api("/api/auditoria/resumen"),
            api("/api/auditoria/actividad-reciente")
        ]);
        state.summary = summary;
        state.recent = recent;
        renderSummary();
        await loadCurrentTab();
        status("Auditoría cargada desde WayPathDB.");
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudo cargar auditoría.");
        status("No se pudo cargar auditoría.");
    }
}

async function loadCurrentTab() {
    if (state.activeTab === "reciente") {
        state.total = totalRecent();
        render();
        return;
    }
    const host = document.getElementById("auditoriaTableHost");
    host.innerHTML = adminLoading("Cargando registros...");
    try {
        const page = await api(endpoint());
        state.total = page.total || 0;
        if (state.activeTab === "accesos") state.accesses = page.items || [];
        if (state.activeTab === "cambios") state.changes = page.items || [];
        if (state.activeTab === "errores") state.errors = page.items || [];
        render();
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudieron cargar los registros.");
    }
}

function endpoint() {
    const params = new URLSearchParams({ page: state.page, pageSize: state.pageSize });
    const filtro = document.getElementById("filtroAuditoria")?.value;
    const desde = document.getElementById("filtroDesdeAuditoria")?.value;
    const hasta = document.getElementById("filtroHastaAuditoria")?.value;
    if (desde) params.set("desde", new Date(desde).toISOString());
    if (hasta) params.set("hasta", new Date(hasta).toISOString());
    if (state.activeTab === "accesos" && ["EXITOSO", "FALLIDO", "BLOQUEADO", "LOGOUT", "REFRESH"].includes(filtro)) params.set("resultado", filtro);
    if (state.activeTab === "cambios" && ["INSERT", "UPDATE", "DELETE"].includes(filtro)) params.set("operacion", filtro);
    if (state.activeTab === "errores" && ["INFO", "WARN", "ERROR", "CRITICAL"].includes(filtro)) params.set("nivel", filtro);
    return `/api/auditoria/${state.activeTab}?${params}`;
}

function renderSummary() {
    const summary = state.summary || {};
    text("auditCambios", summary.cambios);
    text("auditAccesosOk", summary.accesosExitosos);
    text("auditAccesosFail", summary.accesosFallidos);
    text("auditErrores", summary.errores);
    text("auditCriticos", summary.criticos);
}

function render() {
    const host = document.getElementById("auditoriaTableHost");
    const renderers = {
        accesos: renderAccesses,
        cambios: renderChanges,
        errores: renderErrors,
        reciente: renderRecent
    };
    host.innerHTML = renderers[state.activeTab]?.() || renderAccesses();
    bindTableActions(host);
    renderPager();
}

function renderAccesses() {
    const rows = state.accesses.filter(item => matchesText(`${item.usuarioId || ""} ${item.emailIntentado || ""} ${item.resultado} ${item.ip || ""} ${item.userAgent || ""}`));
    if (!rows.length) return adminEmpty("Sin accesos", "No hay accesos que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} accesos visibles`, `Total: ${state.total}`, `
        <table class="wp-table"><thead><tr><th>Usuario</th><th>Email intentado</th><th>Resultado</th><th>IP</th><th>Navegador</th><th>Fecha</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${escapeHtml(item.usuarioId || "-")}</td><td>${escapeHtml(item.emailIntentado || "-")}</td><td>${stateBadge(item.resultado)}</td><td>${escapeHtml(item.ip || "-")}</td><td>${escapeHtml(shortText(item.userAgent || "-", 48))}</td><td>${formatDateTime(item.creadoEn)}</td></tr>`).join("")}</tbody></table>`);
}

function renderChanges() {
    const rows = state.changes.filter(item => matchesText(`${item.usuarioId || ""} ${item.esquemaNombre} ${item.tablaNombre} ${item.clavePrimaria} ${item.operacion}`));
    if (!rows.length) return adminEmpty("Sin cambios", "No hay cambios que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} cambios visibles`, `Total: ${state.total}`, `
        <table class="wp-table"><thead><tr><th>Tabla</th><th>Operación</th><th>Clave</th><th>Usuario</th><th>IP</th><th>Fecha</th><th style="text-align:right;">Acciones</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td><strong>${escapeHtml(item.esquemaNombre)}.${escapeHtml(item.tablaNombre)}</strong></td><td>${stateBadge(item.operacion)}</td><td><code>${escapeHtml(item.clavePrimaria)}</code></td><td>${escapeHtml(item.usuarioId || "-")}</td><td>${escapeHtml(item.ip || "-")}</td><td>${formatDateTime(item.creadoEn)}</td><td><div class="admin-actions-cell"><button class="admin-action-btn" data-action="change-detail" data-id="${escapeHtml(item.auditoriaCambioId)}"><i class="fa-solid fa-eye"></i> Detalle</button></div></td></tr>`).join("")}</tbody></table>`);
}

function renderErrors() {
    const rows = state.errors.filter(item => matchesText(`${item.usuarioId || ""} ${item.nivel} ${item.origen} ${item.mensaje}`));
    if (!rows.length) return adminEmpty("Sin errores", "No hay errores que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} errores visibles`, `Total: ${state.total}`, `
        <table class="wp-table"><thead><tr><th>Nivel</th><th>Origen</th><th>Mensaje</th><th>Usuario</th><th>Fecha</th><th style="text-align:right;">Acciones</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${stateBadge(item.nivel)}</td><td>${escapeHtml(item.origen)}</td><td>${escapeHtml(shortText(item.mensaje, 80))}</td><td>${escapeHtml(item.usuarioId || "-")}</td><td>${formatDateTime(item.creadoEn)}</td><td><div class="admin-actions-cell"><button class="admin-action-btn" data-action="error-detail" data-id="${escapeHtml(item.errorAplicacionId)}"><i class="fa-solid fa-eye"></i> Detalle</button></div></td></tr>`).join("")}</tbody></table>`);
}

function renderRecent() {
    const recent = state.recent || { accesos: [], cambios: [], errores: [] };
    const rows = [
        ...(recent.accesos || []).map(item => ({ tipo: "Acceso", titulo: item.resultado, detalle: item.emailIntentado || item.usuarioId || "-", fecha: item.creadoEn })),
        ...(recent.cambios || []).map(item => ({ tipo: "Cambio", titulo: `${item.esquemaNombre}.${item.tablaNombre}`, detalle: `${item.operacion} · ${item.clavePrimaria}`, fecha: item.creadoEn })),
        ...(recent.errores || []).map(item => ({ tipo: "Error", titulo: item.nivel, detalle: item.mensaje, fecha: item.creadoEn }))
    ].filter(item => matchesText(`${item.tipo} ${item.titulo} ${item.detalle}`)).sort((a, b) => new Date(b.fecha) - new Date(a.fecha));
    if (!rows.length) return adminEmpty("Sin actividad reciente", "No hay eventos recientes para mostrar.");
    return tableCard(`${rows.length} eventos recientes`, "API: /api/auditoria/actividad-reciente", `
        <table class="wp-table"><thead><tr><th>Tipo</th><th>Evento</th><th>Detalle</th><th>Fecha</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${stateBadge(item.tipo)}</td><td><strong>${escapeHtml(item.titulo)}</strong></td><td>${escapeHtml(shortText(item.detalle, 100))}</td><td>${formatDateTime(item.fecha)}</td></tr>`).join("")}</tbody></table>`);
}

function bindTableActions(host) {
    host.querySelectorAll("[data-action='change-detail']").forEach(button => button.addEventListener("click", () => showChange(button.dataset.id)));
    host.querySelectorAll("[data-action='error-detail']").forEach(button => button.addEventListener("click", () => showError(button.dataset.id)));
}

async function showChange(id) {
    try {
        const item = await api(`/api/auditoria/cambios/${encodeURIComponent(id)}`);
        modal("Detalle de cambio", `
            <div class="admin-detail-list">
                <p><strong>Tabla:</strong> ${escapeHtml(item.esquemaNombre)}.${escapeHtml(item.tablaNombre)}</p>
                <p><strong>Operación:</strong> ${escapeHtml(item.operacion)}</p>
                <p><strong>Clave:</strong> ${escapeHtml(item.clavePrimaria)}</p>
                <p><strong>Usuario:</strong> ${escapeHtml(item.usuarioId || "-")}</p>
                <p><strong>Antes:</strong></p><pre>${escapeHtml(pretty(item.datosAntes))}</pre>
                <p><strong>Después:</strong></p><pre>${escapeHtml(pretty(item.datosDespues))}</pre>
            </div>`);
    } catch (error) {
        toast(error.message || "No se pudo cargar el cambio.", "error");
    }
}

async function showError(id) {
    try {
        const item = await api(`/api/auditoria/errores/${encodeURIComponent(id)}`);
        modal("Detalle de error", `
            <div class="admin-detail-list">
                <p><strong>Nivel:</strong> ${escapeHtml(item.nivel)}</p>
                <p><strong>Origen:</strong> ${escapeHtml(item.origen)}</p>
                <p><strong>Usuario:</strong> ${escapeHtml(item.usuarioId || "-")}</p>
                <p><strong>Mensaje:</strong> ${escapeHtml(item.mensaje)}</p>
                <p><strong>Datos:</strong></p><pre>${escapeHtml(pretty(item.datosJson))}</pre>
                <p><strong>StackTrace:</strong></p><pre>${escapeHtml(item.stackTrace || "No disponible")}</pre>
            </div>`);
    } catch (error) {
        toast(error.message || "No se pudo cargar el error.", "error");
    }
}

function changePage(delta) {
    const lastPage = Math.max(1, Math.ceil(state.total / state.pageSize));
    const next = Math.min(lastPage, Math.max(1, state.page + delta));
    if (next === state.page || state.activeTab === "reciente") return;
    state.page = next;
    loadCurrentTab();
}

function renderPager() {
    const pager = document.querySelector(".admin-pager");
    if (!pager) return;
    const isRecent = state.activeTab === "reciente";
    pager.hidden = isRecent;
    const lastPage = Math.max(1, Math.ceil(state.total / state.pageSize));
    document.getElementById("auditPageInfo").textContent = `Página ${state.page} de ${lastPage}`;
    document.getElementById("auditPrev").disabled = state.page <= 1;
    document.getElementById("auditNext").disabled = state.page >= lastPage;
}

function matchesText(value) {
    const text = document.getElementById("buscarAuditoria")?.value.trim().toLowerCase() || "";
    return String(value || "").toLowerCase().includes(text);
}

function tableCard(left, right, table) {
    return `<div class="admin-table-card"><div class="admin-table-meta"><span>${escapeHtml(left)}</span><span>${escapeHtml(right)}</span></div><div class="wp-table-wrap">${table}</div></div>`;
}

function totalRecent() {
    return (state.recent?.accesos?.length || 0) + (state.recent?.cambios?.length || 0) + (state.recent?.errores?.length || 0);
}

function text(id, value) {
    const element = document.getElementById(id);
    if (element) element.textContent = Number(value || 0).toLocaleString("es-PE");
}

function pretty(value) {
    if (!value) return "No disponible";
    try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; }
}

function modal(title, html) {
    if (window.Swal) Swal.fire({ title, html, width: 860, confirmButtonColor: "#f97316" });
    else alert(html.replace(/<[^>]+>/g, "\n"));
}

function toast(message, icon = "success") {
    if (window.Swal) Swal.fire({ text: message, icon, timer: 1800, showConfirmButton: false });
    else alert(message);
}

function shortText(value, length) {
    const text = String(value || "");
    return text.length > length ? `${text.slice(0, length)}...` : text;
}

function formatDateTime(value) {
    if (!value) return "-";
    return new Date(value).toLocaleString("es-PE", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function status(message) {
    const element = document.getElementById("auditoriaStatus");
    if (element) element.textContent = message;
}
