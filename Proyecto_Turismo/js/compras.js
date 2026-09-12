import { api, ApiError } from "./api.js";

const fallbackImage = "Img/machupichu.jpg";
let purchases = [];
let currentFilter = "all";

document.addEventListener("DOMContentLoaded", () => {
    bindTabs();
    bindModal();
    loadPurchases();
});

async function loadPurchases() {
    const container = document.getElementById("contenedor-compras");
    if (!container) return;
    container.innerHTML = state("Cargando tus compras...");
    try {
        purchases = await api("/api/ventas/compras/me");
        renderPurchases();
    } catch (error) {
        const message = error instanceof ApiError && error.status === 401
            ? "Inicia sesion para ver tus compras reales."
            : error.message || "No se pudieron cargar tus compras.";
        container.innerHTML = state(message);
        updateStats([]);
    }
}

function renderPurchases() {
    const container = document.getElementById("contenedor-compras");
    const visible = purchases.filter(x => currentFilter === "all" || statusClass(x.estado) === currentFilter);
    updateStats(purchases);
    if (visible.length === 0) {
        container.innerHTML = state(currentFilter === "all"
            ? "Aun no tienes compras emitidas."
            : "No hay compras en este estado.");
        return;
    }
    container.innerHTML = visible.map(cardHtml).join("");
    container.querySelectorAll(".pc-detail-btn").forEach(btn => {
        btn.addEventListener("click", () => openDetails(btn.dataset.id));
    });
}

function cardHtml(item) {
    const status = statusClass(item.estado);
    return `
        <article class="purchase-card" data-status="${status}">
            <img src="${fallbackImage}" alt="${escapeHtml(item.destino)}">
            <div class="purchase-info">
                <h3>${escapeHtml(item.paquete)}</h3>
                <div class="purchase-meta">
                    <span><i class="fa-solid fa-map-location-dot"></i> ${escapeHtml(item.destino)}</span>
                    <span><i class="fa-regular fa-calendar"></i> ${formatDate(item.fechaInicio)} - ${formatDate(item.fechaFin)}</span>
                </div>
                <p>Reserva ${escapeHtml(item.codigoReserva)}</p>
                <span class="purchase-code">${escapeHtml(item.codigoCompra)}</span>
            </div>
            <div class="purchase-side">
                <span class="status-pill ${status}">${escapeHtml(item.estado)}</span>
                <strong>${money(item.totalPagado)}</strong>
                <button class="pc-detail-btn" type="button" data-id="${item.compraId}">
                    Ver detalles
                </button>
            </div>
        </article>`;
}

async function openDetails(id) {
    try {
        const detail = await api(`/api/ventas/compras/me/${id}`);
        document.getElementById("modalTitle").textContent = detail.paquete;
        document.getElementById("modalImg").src = fallbackImage;
        document.getElementById("modalImg").alt = detail.destino;
        document.getElementById("modalPrecio").textContent = money(detail.totalPagado);
        document.getElementById("modalDetalles").innerHTML = [
            `Destino: ${escapeHtml(detail.destino)}`,
            `Reserva: ${escapeHtml(detail.codigoReserva)}`,
            `Comprobante: ${detail.comprobante ? `${escapeHtml(detail.comprobante.serie)}-${escapeHtml(detail.comprobante.numero)}` : "pendiente"}`,
            `Tickets emitidos: ${detail.tickets?.length ?? 0}`,
            `Voucher: ${detail.voucher ? escapeHtml(detail.voucher.codigoVoucher) : "pendiente"}`
        ].join("<br>");
        showModal(true);
    } catch (error) {
        alert(error.message || "No se pudo cargar el detalle.");
    }
}

function bindTabs() {
    document.querySelectorAll(".tab-btn").forEach(btn => {
        btn.addEventListener("click", () => {
            document.querySelectorAll(".tab-btn").forEach(item => {
                item.classList.remove("active");
                item.setAttribute("aria-selected", "false");
            });
            btn.classList.add("active");
            btn.setAttribute("aria-selected", "true");
            currentFilter = btn.dataset.filter || "all";
            renderPurchases();
        });
    });
}

function bindModal() {
    document.getElementById("modalClose")?.addEventListener("click", () => showModal(false));
    document.querySelector(".wp-modal-backdrop")?.addEventListener("click", () => showModal(false));
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") showModal(false);
    });
}

function showModal(open) {
    const modal = document.getElementById("detailModal");
    if (!modal) return;
    modal.style.display = open ? "flex" : "none";
    document.body.style.overflow = open ? "hidden" : "";
}

function updateStats(items) {
    const stats = document.querySelectorAll(".cstat-num");
    if (stats[0]) stats[0].textContent = items.length.toString();
    if (stats[1]) stats[1].textContent = items.filter(x => statusClass(x.estado) === "proxima").length.toString();
    if (stats[2]) stats[2].textContent = items.filter(x => statusClass(x.estado) === "completado").length.toString();
}

function statusClass(value) {
    const state = (value || "").toUpperCase();
    if (state === "ANULADA") return "cancelado";
    if (state === "COMPLETADA") return "completado";
    return "proxima";
}

function money(value) {
    return Number(value || 0).toLocaleString("es-PE", { style: "currency", currency: "PEN" });
}

function formatDate(value) {
    if (!value) return "Fecha por definir";
    return new Date(`${value}T00:00:00`).toLocaleDateString("es-PE", { day: "2-digit", month: "short", year: "numeric" });
}

function state(text) {
    return `<div class="wp-empty-state"><i class="fa-solid fa-bag-shopping" aria-hidden="true"></i><strong>${escapeHtml(text)}</strong><p>WayPath mostrara aqui las compras generadas desde pagos aprobados.</p></div>`;
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, char => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    })[char]);
}
