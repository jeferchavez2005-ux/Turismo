import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = {
    activeTab: "compras",
    purchases: [],
    details: new Map(),
    reviews: [],
    page: 1,
    pageSize: 100,
    totalPurchases: 0,
    totalReviews: 0,
    user: null
};

document.addEventListener("DOMContentLoaded", init);

async function init() {
    try {
        state.user = await initAdminPage({ active: "Ventas", title: "Ventas" });
    } catch {
        return;
    }
    if (!can(state.user, "VENTAS_RESERVAS_LEER")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.querySelectorAll(".admin-tab").forEach(button => {
        button.addEventListener("click", () => {
            state.activeTab = button.dataset.tab || "compras";
            document.querySelectorAll(".admin-tab").forEach(tab => tab.classList.toggle("active", tab === button));
            render();
        });
    });
    document.getElementById("buscarVentas")?.addEventListener("input", render);
    document.getElementById("filtroEstadoVentas")?.addEventListener("change", render);
    document.getElementById("recargarVentas")?.addEventListener("click", loadData);
}

async function loadData() {
    const host = document.getElementById("ventasTableHost");
    host.innerHTML = adminLoading("Cargando ventas...");
    try {
        const [purchasePage, reviewPage] = await Promise.all([
            api(`/api/ventas/admin/compras?page=${state.page}&pageSize=${state.pageSize}`),
            api(`/api/ventas/admin/resenas?page=${state.page}&pageSize=${state.pageSize}`)
        ]);
        state.purchases = purchasePage.items || [];
        state.reviews = reviewPage.items || [];
        state.totalPurchases = purchasePage.total || state.purchases.length;
        state.totalReviews = reviewPage.total || state.reviews.length;
        await loadPurchaseDetails();
        render();
        status(`Mostrando ${state.purchases.length} compras y ${state.reviews.length} reseñas.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudieron cargar las ventas.");
        status("No se pudieron cargar las ventas.");
    }
}

async function loadPurchaseDetails() {
    state.details.clear();
    await Promise.all(state.purchases.map(async purchase => {
        try {
            const detail = await api(`/api/ventas/admin/compras/${encodeURIComponent(purchase.compraId)}`);
            state.details.set(String(purchase.compraId), detail);
        } catch {
            state.details.set(String(purchase.compraId), null);
        }
    }));
}

function render() {
    const host = document.getElementById("ventasTableHost");
    const renderers = {
        compras: renderPurchases,
        comprobantes: renderReceipts,
        tickets: renderTickets,
        vouchers: renderVouchers,
        resenas: renderReviews
    };
    host.innerHTML = renderers[state.activeTab]?.() || renderPurchases();
    bindTableActions(host);
}

function renderPurchases() {
    const rows = filteredPurchases();
    if (!rows.length) return adminEmpty("Sin compras", "No hay compras que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} compras visibles`, `Total: ${state.totalPurchases}`, `
        <table class="wp-table">
            <thead><tr><th>Código compra</th><th>Cliente</th><th>Reserva</th><th>Pago</th><th>Monto</th><th>Estado</th><th>Fecha</th><th style="text-align:right;">Acciones</th></tr></thead>
            <tbody>${rows.map(purchaseRow).join("")}</tbody>
        </table>`);
}

function purchaseRow(purchase) {
    const detail = state.details.get(String(purchase.compraId));
    return `
        <tr>
            <td><strong>${escapeHtml(purchase.codigoCompra)}</strong></td>
            <td>${escapeHtml(purchase.cliente || "Cliente")}</td>
            <td><div class="admin-row-title"><strong>${escapeHtml(purchase.codigoReserva)}</strong><span>ID ${escapeHtml(purchase.reservaId)}</span></div></td>
            <td>${relatedPayment(detail)}</td>
            <td>${money(purchase.totalPagado)}</td>
            <td>${stateBadge(purchase.estado)}</td>
            <td>${formatDate(purchase.fechaCompra)}</td>
            <td><div class="admin-actions-cell">
                <button class="admin-action-btn" data-action="purchase-detail" data-id="${escapeHtml(purchase.compraId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                ${can(state.user, "VENTAS_RESERVAS_GESTIONAR") ? `<button class="admin-action-btn" data-action="purchase-state" data-id="${escapeHtml(purchase.compraId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>` : ""}
            </div></td>
        </tr>`;
}

function renderReceipts() {
    const rows = filteredReceipts();
    if (!rows.length) return adminEmpty("Sin comprobantes", "No hay comprobantes generados para los filtros actuales.");
    return tableCard(`${rows.length} comprobantes visibles`, "Desde detalle de compras", `
        <table class="wp-table">
            <thead><tr><th>Tipo</th><th>Serie</th><th>Número</th><th>Cliente</th><th>Monto</th><th>Estado</th><th style="text-align:right;">Acciones</th></tr></thead>
            <tbody>${rows.map(receiptRow).join("")}</tbody>
        </table>`);
}

function receiptRow(item) {
    const receipt = item.comprobante;
    return `
        <tr>
            <td>${escapeHtml(receipt.tipoComprobante)}</td>
            <td>${escapeHtml(receipt.serie)}</td>
            <td>${escapeHtml(receipt.numero)}</td>
            <td>${escapeHtml(item.cliente)}</td>
            <td>${money(receipt.total)}</td>
            <td>${stateBadge(receipt.estado)}</td>
            <td><div class="admin-actions-cell">
                <button class="admin-action-btn" data-action="receipt-detail" data-id="${escapeHtml(item.compraId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                ${can(state.user, "VENTAS_RESERVAS_GESTIONAR") ? `<button class="admin-action-btn" data-action="receipt-state" data-id="${escapeHtml(receipt.comprobanteId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>` : ""}
            </div></td>
        </tr>`;
}

function renderTickets() {
    const rows = filteredTickets();
    if (!rows.length) return adminEmpty("Sin tickets", "No hay tickets generados para los filtros actuales.");
    return tableCard(`${rows.length} tickets visibles`, "Emitidos por pasajero", `
        <table class="wp-table">
            <thead><tr><th>Pasajero</th><th>Salida</th><th>Código ticket</th><th>Compra</th><th>Estado</th><th>Emisión</th></tr></thead>
            <tbody>${rows.map(ticketRow).join("")}</tbody>
        </table>`);
}

function ticketRow(item) {
    return `
        <tr>
            <td>${escapeHtml(item.pasajero)}</td>
            <td><code>${escapeHtml(item.reservaId)}</code></td>
            <td><strong>${escapeHtml(item.codigoTicket)}</strong></td>
            <td><code>${escapeHtml(item.compraId)}</code></td>
            <td>${stateBadge(item.estado)}</td>
            <td>${formatDate(item.emitidoEn)}</td>
        </tr>`;
}

function renderVouchers() {
    const rows = filteredVouchers();
    if (!rows.length) return adminEmpty("Sin vouchers", "No hay vouchers generados para los filtros actuales.");
    return tableCard(`${rows.length} vouchers visibles`, "Emitidos por compra", `
        <table class="wp-table">
            <thead><tr><th>Código voucher</th><th>Compra</th><th>Reserva</th><th>Estado</th><th>Fecha emisión</th></tr></thead>
            <tbody>${rows.map(voucherRow).join("")}</tbody>
        </table>`);
}

function voucherRow(item) {
    return `
        <tr>
            <td><strong>${escapeHtml(item.codigoVoucher)}</strong></td>
            <td><code>${escapeHtml(item.compraId)}</code></td>
            <td><code>${escapeHtml(item.reservaId)}</code></td>
            <td>${stateBadge(item.estado)}</td>
            <td>${formatDate(item.emitidoEn)}</td>
        </tr>`;
}

function renderReviews() {
    const rows = filteredReviews();
    if (!rows.length) return adminEmpty("Sin reseñas", "No hay reseñas que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} reseñas visibles`, `Total: ${state.totalReviews}`, `
        <table class="wp-table">
            <thead><tr><th>Cliente</th><th>Destino</th><th>Puntuación</th><th>Comentario</th><th>Estado</th><th>Fecha</th><th style="text-align:right;">Acciones</th></tr></thead>
            <tbody>${rows.map(reviewRow).join("")}</tbody>
        </table>`);
}

function reviewRow(review) {
    const manage = can(state.user, "VENTAS_RESERVAS_GESTIONAR");
    return `
        <tr>
            <td>${escapeHtml(review.cliente)}</td>
            <td>${escapeHtml(review.destino)}</td>
            <td>${"★".repeat(Number(review.calificacion || 0))}</td>
            <td><div class="admin-row-title"><strong>${escapeHtml(review.titulo || "Sin título")}</strong><span>${escapeHtml(shortText(review.comentario, 78))}</span></div></td>
            <td>${stateBadge(review.estadoModeracion)}</td>
            <td>${formatDate(review.creadoEn)}</td>
            <td><div class="admin-actions-cell">
                <button class="admin-action-btn" data-action="review-detail" data-id="${escapeHtml(review.resenaId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                ${manage ? `<button class="admin-action-btn" data-action="review-approve" data-id="${escapeHtml(review.resenaId)}"><i class="fa-solid fa-check"></i> Aprobar</button>` : ""}
                ${manage ? `<button class="admin-action-btn danger" data-action="review-reject" data-id="${escapeHtml(review.resenaId)}"><i class="fa-solid fa-ban"></i> Rechazar</button>` : ""}
            </div></td>
        </tr>`;
}

function filteredPurchases() {
    return state.purchases.filter(matchesCommon);
}

function filteredReceipts() {
    return purchaseDetails()
        .filter(item => item.comprobante)
        .filter(item => matchesText(`${item.comprobante.tipoComprobante} ${item.comprobante.serie} ${item.comprobante.numero} ${item.cliente} ${item.comprobante.estado}`) && matchesState(item.comprobante.estado));
}

function filteredTickets() {
    return purchaseDetails().flatMap(item => item.tickets || [])
        .filter(item => matchesText(`${item.codigoTicket} ${item.pasajero} ${item.reservaId} ${item.estado}`) && matchesState(item.estado));
}

function filteredVouchers() {
    return purchaseDetails().map(item => item.voucher).filter(Boolean)
        .filter(item => matchesText(`${item.codigoVoucher} ${item.compraId} ${item.reservaId} ${item.estado}`) && matchesState(item.estado));
}

function filteredReviews() {
    return state.reviews.filter(review => matchesText(`${review.cliente} ${review.destino} ${review.titulo || ""} ${review.comentario} ${review.estadoModeracion}`) && matchesState(review.estadoModeracion));
}

function matchesCommon(purchase) {
    return matchesText(`${purchase.codigoCompra} ${purchase.codigoReserva} ${purchase.cliente} ${purchase.paquete} ${purchase.destino} ${purchase.estado}`) && matchesState(purchase.estado);
}

function matchesText(value) {
    const text = document.getElementById("buscarVentas")?.value.trim().toLowerCase() || "";
    return String(value || "").toLowerCase().includes(text);
}

function matchesState(value) {
    const statusFilter = document.getElementById("filtroEstadoVentas")?.value || "";
    return !statusFilter || value === statusFilter;
}

function purchaseDetails() {
    return [...state.details.values()].filter(Boolean);
}

function bindTableActions(host) {
    host.querySelectorAll("[data-action='purchase-detail']").forEach(button => button.addEventListener("click", () => showPurchaseDetail(button.dataset.id)));
    host.querySelectorAll("[data-action='purchase-state']").forEach(button => button.addEventListener("click", () => changePurchaseState(button.dataset.id)));
    host.querySelectorAll("[data-action='receipt-detail']").forEach(button => button.addEventListener("click", () => showPurchaseDetail(button.dataset.id)));
    host.querySelectorAll("[data-action='receipt-state']").forEach(button => button.addEventListener("click", () => changeReceiptState(button.dataset.id)));
    host.querySelectorAll("[data-action='review-detail']").forEach(button => button.addEventListener("click", () => showReviewDetail(button.dataset.id)));
    host.querySelectorAll("[data-action='review-approve']").forEach(button => button.addEventListener("click", () => moderateReview(button.dataset.id, "APROBADA")));
    host.querySelectorAll("[data-action='review-reject']").forEach(button => button.addEventListener("click", () => moderateReview(button.dataset.id, "RECHAZADA")));
}

function showPurchaseDetail(id) {
    const detail = state.details.get(String(id));
    if (!detail) {
        toast("No se pudo cargar el detalle de la compra.", "error");
        return;
    }
    const receipt = detail.comprobante;
    const html = `
        <div class="admin-detail-list">
            <p><strong>Compra:</strong> ${escapeHtml(detail.codigoCompra)} (${escapeHtml(detail.estado)})</p>
            <p><strong>Cliente:</strong> ${escapeHtml(detail.cliente)}</p>
            <p><strong>Reserva:</strong> ${escapeHtml(detail.codigoReserva)}</p>
            <p><strong>Destino:</strong> ${escapeHtml(detail.destino)} - ${escapeHtml(detail.paquete)}</p>
            <p><strong>Total pagado:</strong> ${money(detail.totalPagado)}</p>
            <p><strong>Comprobante:</strong> ${receipt ? `${escapeHtml(receipt.tipoComprobante)} ${escapeHtml(receipt.serie)}-${escapeHtml(receipt.numero)} · ${money(receipt.total)}` : "No disponible"}</p>
            <p><strong>Tickets:</strong> ${(detail.tickets || []).map(ticket => `${escapeHtml(ticket.codigoTicket)} - ${escapeHtml(ticket.pasajero)}`).join("<br>") || "Sin tickets"}</p>
            <p><strong>Voucher:</strong> ${detail.voucher ? escapeHtml(detail.voucher.codigoVoucher) : "Sin voucher"}</p>
        </div>`;
    modal("Detalle de compra", html);
}

function showReviewDetail(id) {
    const review = state.reviews.find(item => String(item.resenaId) === String(id));
    if (!review) return;
    const html = `
        <div class="admin-detail-list">
            <p><strong>Cliente:</strong> ${escapeHtml(review.cliente)}</p>
            <p><strong>Destino:</strong> ${escapeHtml(review.destino)}</p>
            <p><strong>Reserva:</strong> ${escapeHtml(review.reservaId || "No disponible")}</p>
            <p><strong>Puntuación:</strong> ${escapeHtml(review.calificacion)} / 5</p>
            <p><strong>Título:</strong> ${escapeHtml(review.titulo || "Sin título")}</p>
            <p><strong>Comentario:</strong> ${escapeHtml(review.comentario)}</p>
            <p><strong>Estado:</strong> ${escapeHtml(review.estadoModeracion)}</p>
        </div>`;
    modal("Detalle de reseña", html);
}

async function changePurchaseState(id) {
    const result = await statePrompt("Cambiar estado de compra", ["EMITIDA", "ANULADA"]);
    if (!result) return;
    try {
        await api(`/api/ventas/admin/compras/${encodeURIComponent(id)}/estado`, { method: "PUT", body: { codigo: result } });
        toast("Estado de compra actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo actualizar la compra.", "error");
    }
}

async function changeReceiptState(id) {
    const result = await statePrompt("Cambiar estado de comprobante", ["EMITIDA", "ANULADA"]);
    if (!result) return;
    try {
        await api(`/api/ventas/admin/comprobantes/${encodeURIComponent(id)}/estado`, { method: "PUT", body: { codigo: result } });
        toast("Estado de comprobante actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo actualizar el comprobante.", "error");
    }
}

async function moderateReview(id, estado) {
    const result = await Swal.fire({
        title: estado === "APROBADA" ? "Aprobar reseña" : "Rechazar reseña",
        input: "textarea",
        inputPlaceholder: "Comentario de moderación",
        showCancelButton: true,
        confirmButtonText: "Confirmar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316"
    });
    if (!result.isConfirmed) return;
    try {
        await api(`/api/ventas/admin/resenas/${encodeURIComponent(id)}/moderacion`, { method: "PUT", body: { estado, comentario: result.value || null } });
        toast("Reseña moderada.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo moderar la reseña.", "error");
    }
}

async function statePrompt(title, values) {
    const options = values.map(value => `<option value="${value}">${value}</option>`).join("");
    const result = await Swal.fire({
        title,
        html: `<select id="adminVentaEstado" class="swal2-input">${options}</select>`,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: () => document.getElementById("adminVentaEstado").value
    });
    return result.isConfirmed ? result.value : null;
}

function tableCard(left, right, table) {
    return `<div class="admin-table-card"><div class="admin-table-meta"><span>${escapeHtml(left)}</span><span>${escapeHtml(right)}</span></div><div class="wp-table-wrap">${table}</div></div>`;
}

function relatedPayment() {
    return '<span class="admin-muted">Desde reserva/pago</span>';
}

function modal(title, html) {
    if (window.Swal) Swal.fire({ title, html, width: 780, confirmButtonColor: "#f97316" });
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

function formatDate(value) {
    if (!value) return "-";
    return new Date(value).toLocaleDateString("es-PE", { day: "2-digit", month: "short", year: "numeric" });
}

function money(value) {
    return new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(Number(value || 0));
}

function status(message) {
    const element = document.getElementById("ventasStatus");
    if (element) element.textContent = message;
}
