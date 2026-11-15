import { api, ApiError } from "./api.js";
import { errorMessageWithTrace, t } from "./frontend-utils.js";
import { getDestinationImage } from "./destination-images.js";

let purchases = [], destinations = [], currentFilter = "all";
document.getElementById("contenedor-compras")?.replaceChildren();

document.addEventListener("DOMContentLoaded", () => { bindTabs(); bindModal(); loadPurchases(); });

async function loadPurchases() {
    const container = document.getElementById("contenedor-compras");
    container.innerHTML = emptyState(t("purchases.loading", "Cargando tus compras..."));
    try {
        [purchases, destinations] = await Promise.all([api("/api/ventas/compras/me"), api("/api/catalogo/destinos")]);
        renderPurchases();
    } catch (error) {
        const message = error instanceof ApiError && error.status === 401 ? t("purchases.authRequired", "Inicia sesión para ver tus compras.") : errorMessageWithTrace(error, t("purchases.loadError", "No se pudieron cargar tus compras."));
        container.innerHTML = emptyState(message); updateStats([]);
    }
}

function renderPurchases() {
    const visible = purchases.filter(item => currentFilter === "all" || statusClass(item.estado) === currentFilter);
    updateStats(purchases);
    document.getElementById("contenedor-compras").innerHTML = visible.length ? visible.map(cardHtml).join("") : emptyState(t(currentFilter === "all" ? "purchases.empty" : "purchases.emptyFiltered", "No hay compras para mostrar."));
    document.querySelectorAll(".pc-detail-btn").forEach(button => button.addEventListener("click", () => openReceipt(button.dataset.id)));
}

function cardHtml(item) {
    const status = statusClass(item.estado);
    const image = getDestinationImage({ destino: item.destino }, destinations);
    return `<article class="purchase-card" data-status="${status}"><img src="${escapeHtml(image)}" alt="${escapeHtml(item.destino)}" loading="lazy"><div class="purchase-info"><h3>${escapeHtml(item.paquete)}</h3><div class="purchase-meta"><span><i class="fa-solid fa-map-location-dot" aria-hidden="true"></i> ${escapeHtml(item.destino)}</span><span><i class="fa-regular fa-calendar" aria-hidden="true"></i> ${formatDate(item.fechaInicio)} – ${formatDate(item.fechaFin)}</span></div><p>${escapeHtml(t("purchases.booking", "Reserva"))} ${escapeHtml(item.codigoReserva)}</p><span class="purchase-code">${escapeHtml(item.codigoCompra)}</span></div><div class="purchase-side"><span class="status-pill ${status}">${escapeHtml(translateState(item.estado))}</span><strong>${money(item.totalPagado)}</strong><button class="pc-detail-btn" type="button" data-id="${item.compraId}">${escapeHtml(t("purchases.viewReceipt", "Ver comprobante de compra"))}</button></div></article>`;
}

async function openReceipt(id) {
    try {
        const detail = await api(`/api/ventas/compras/me/${id}`);
        document.getElementById("modalTitle").textContent = t("purchases.receiptTitle", "Comprobante de compra");
        const image = document.getElementById("modalImg"); image.src = getDestinationImage({ destino: detail.destino }, destinations); image.alt = detail.destino;
        document.getElementById("modalPrecio").textContent = money(detail.totalPagado);
        document.getElementById("modalDetalles").innerHTML = receiptHtml(detail);
        ensurePrintActions(); showModal(true);
    } catch (error) { alert(errorMessageWithTrace(error, t("purchases.detailError", "No se pudo cargar el comprobante."))); }
}

function receiptHtml(detail) {
    const travelers = detail.tickets?.length || null;
    const receiptState = detail.comprobante?.estado || null;
    return `<div class="purchase-receipt"><strong class="receipt-brand">WAYPATH</strong><dl>
      ${row(t("purchases.purchase", "Compra"), detail.codigoCompra)}${row(t("purchases.booking", "Reserva"), detail.codigoReserva)}${row(t("purchases.customer", "Cliente"), detail.cliente)}${row(t("purchases.destination", "Destino"), detail.destino)}${row(t("purchases.experience", "Experiencia"), detail.paquete)}${row(t("purchases.travelDate", "Fecha del viaje"), `${formatDate(detail.fechaInicio)} – ${formatDate(detail.fechaFin)}`)}${travelers ? row(t("purchases.travelers", "Viajeros"), travelers) : ""}${row(t("purchases.purchaseState", "Estado de compra"), translateState(detail.estado))}${receiptState ? row(t("purchases.receiptState", "Estado del comprobante"), translateState(receiptState)) : ""}</dl><p class="receipt-disclaimer">${escapeHtml(t("purchases.internalDisclaimer", "Comprobante interno de compra de WayPath. No constituye comprobante electrónico SUNAT."))}</p></div>`;
}

function row(label, value) { return `<div><dt>${escapeHtml(label)}</dt><dd>${escapeHtml(value)}</dd></div>`; }
function ensurePrintActions() { const body = document.querySelector(".wp-modal-body"); if (body.querySelector(".receipt-actions")) return; const actions = document.createElement("div"); actions.className = "receipt-actions"; actions.innerHTML = `<button type="button" class="wp-btn wp-btn-primary" id="printReceipt"><i class="fa-solid fa-print" aria-hidden="true"></i> ${escapeHtml(t("purchases.print", "Imprimir"))}</button>`; body.append(actions); actions.querySelector("button").addEventListener("click", () => window.print()); }

function bindTabs() { document.querySelectorAll(".tab-btn").forEach(button => button.addEventListener("click", () => { document.querySelectorAll(".tab-btn").forEach(item => { item.classList.remove("active"); item.setAttribute("aria-selected", "false"); }); button.classList.add("active"); button.setAttribute("aria-selected", "true"); currentFilter = button.dataset.filter || "all"; renderPurchases(); })); }
function bindModal() { document.getElementById("modalClose")?.addEventListener("click", () => showModal(false)); document.querySelector(".wp-modal-backdrop")?.addEventListener("click", () => showModal(false)); document.addEventListener("keydown", event => { if (event.key === "Escape") showModal(false); }); }
function showModal(open) { const modal = document.getElementById("detailModal"); modal.style.display = open ? "flex" : "none"; document.body.style.overflow = open ? "hidden" : ""; }
function updateStats(items) { const stats = document.querySelectorAll(".cstat-num"); if (stats[0]) stats[0].textContent = items.length; if (stats[1]) stats[1].textContent = items.filter(item => statusClass(item.estado) === "proxima").length; if (stats[2]) stats[2].textContent = items.filter(item => statusClass(item.estado) === "completado").length; }
function statusClass(value) { const status = String(value || "").toUpperCase(); if (["ANULADA","CANCELADA"].includes(status)) return "cancelado"; if (status === "COMPLETADA") return "completado"; return "proxima"; }
function translateState(value) { const key = `states.${String(value || "unknown").toLowerCase()}`; return t(key, String(value || t("states.unknown", "Sin estado")).replaceAll("_", " ")); }
function money(value) { return Number(value || 0).toLocaleString(document.documentElement.lang || "es-PE", { style: "currency", currency: "PEN" }); }
function formatDate(value) { return value ? new Date(`${value}T00:00:00`).toLocaleDateString(document.documentElement.lang || "es-PE", { dateStyle: "medium" }) : t("purchases.datePending", "Fecha por definir"); }
function emptyState(message) { return `<div class="wp-empty-state"><i class="fa-solid fa-bag-shopping" aria-hidden="true"></i><strong>${escapeHtml(message)}</strong><p>${escapeHtml(t("purchases.emptyHelp", "Aquí aparecerán únicamente compras generadas desde pagos confirmados."))}</p></div>`; }
function escapeHtml(value) { return String(value ?? "").replace(/[&<>"']/g, char => ({"&":"&amp;","<":"&lt;",">":"&gt;","\"":"&quot;","'":"&#39;"})[char]); }
window.addEventListener("waypath:languagechange", renderPurchases);
