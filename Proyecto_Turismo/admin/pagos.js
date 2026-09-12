import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = { payments: [], orders: [], reservations: new Map(), page: 1, pageSize: 100, totalPayments: 0, totalOrders: 0, user: null };

document.addEventListener("DOMContentLoaded", init);

async function init() {
    try {
        state.user = await initAdminPage({ active: "Pagos", title: "Pagos" });
    } catch {
        return;
    }
    if (!can(state.user, "PAGOS_LEER")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.getElementById("buscarPagos")?.addEventListener("input", render);
    document.getElementById("filtroEstadoPagos")?.addEventListener("change", render);
    document.getElementById("recargarPagos")?.addEventListener("click", loadData);
}

async function loadData() {
    const host = document.getElementById("pagosTableHost");
    host.innerHTML = adminLoading("Cargando pagos...");
    try {
        const [paymentsPage, ordersPage] = await Promise.all([
            api(`/api/pagos/admin/pagos?page=${state.page}&pageSize=${state.pageSize}`),
            api(`/api/pagos/admin/ordenes?page=${state.page}&pageSize=${state.pageSize}`)
        ]);
        state.payments = paymentsPage.items || [];
        state.orders = ordersPage.items || [];
        state.totalPayments = paymentsPage.total || state.payments.length;
        state.totalOrders = ordersPage.total || state.orders.length;
        await loadReservationContext();
        render();
        status(`Mostrando ${state.payments.length} pagos y ${state.orders.length} órdenes.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudieron cargar los pagos.");
        status("No se pudieron cargar los pagos.");
    }
}

function render() {
    const host = document.getElementById("pagosTableHost");
    const rows = filteredPayments();
    const orders = filteredOrders();
    if (!rows.length && !orders.length) {
        host.innerHTML = adminEmpty("Sin pagos", "No hay pagos u órdenes que coincidan con los filtros actuales.");
        return;
    }
    host.innerHTML = `
        <div class="admin-grid two">
            <article class="admin-table-card">
                <div class="admin-table-meta"><span>${rows.length} pagos visibles</span><span>Total: ${state.totalPayments}</span></div>
                <div class="wp-table-wrap">
                    <table class="wp-table">
                        <thead>
                            <tr>
                                <th>Pago</th>
                                <th>Reserva</th>
                                <th>Cliente</th>
                                <th>Método</th>
                                <th>Monto</th>
                                <th>Estado</th>
                                <th>Fecha</th>
                                <th style="text-align:right;">Acciones</th>
                            </tr>
                        </thead>
                        <tbody>${rows.map(paymentRow).join("")}</tbody>
                    </table>
                </div>
            </article>
            <article class="admin-table-card">
                <div class="admin-table-meta"><span>${orders.length} órdenes visibles</span><span>Total: ${state.totalOrders}</span></div>
                <div class="wp-table-wrap">
                    <table class="wp-table">
                        <thead>
                            <tr>
                                <th>Orden</th>
                                <th>Reserva</th>
                                <th>Cliente</th>
                                <th>Monto</th>
                                <th>Estado</th>
                                <th>Fecha</th>
                            </tr>
                        </thead>
                        <tbody>${orders.map(orderRow).join("")}</tbody>
                    </table>
                </div>
            </article>
        </div>`;
    host.querySelectorAll("[data-action='detail']").forEach(button => button.addEventListener("click", () => showPaymentDetail(button.dataset.id)));
    host.querySelectorAll("[data-action='state']").forEach(button => button.addEventListener("click", () => changePaymentState(button.dataset.id)));
}

function paymentRow(payment) {
    const manage = can(state.user, "PAGOS_GESTIONAR");
    return `
        <tr>
            <td><div class="admin-row-title"><strong>ID ${escapeHtml(payment.pagoId)}</strong><span>${escapeHtml(payment.codigoTransaccion || "Sin transacción")}</span></div></td>
            <td><code>${escapeHtml(payment.reservaId)}</code></td>
            <td>${reservationClient(payment.reservaId)}</td>
            <td>${escapeHtml(payment.metodoPago || "No disponible")}</td>
            <td>${money(payment.monto, payment.moneda)}</td>
            <td>${stateBadge(payment.estado)}</td>
            <td>${formatDate(payment.fechaPago || payment.creadoEn)}</td>
            <td>
                <div class="admin-actions-cell">
                    <button class="admin-action-btn" data-action="detail" data-id="${escapeHtml(payment.pagoId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                    ${manage ? `<button class="admin-action-btn" data-action="state" data-id="${escapeHtml(payment.pagoId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>` : ""}
                </div>
            </td>
        </tr>`;
}

function orderRow(order) {
    return `
        <tr>
            <td><div class="admin-row-title"><strong>${escapeHtml(order.codigoOrden)}</strong><span>ID ${escapeHtml(order.ordenPagoId)}</span></div></td>
            <td><code>${escapeHtml(order.reservaId)}</code></td>
            <td>${reservationClient(order.reservaId)}</td>
            <td>${money(order.montoTotal, order.moneda)}</td>
            <td>${stateBadge(order.estado)}</td>
            <td>${formatDate(order.fechaCreacion)}</td>
        </tr>`;
}

function filteredPayments() {
    const text = searchText();
    const statusFilter = document.getElementById("filtroEstadoPagos")?.value || "";
    return state.payments.filter(payment => {
        const searchable = `${payment.pagoId || ""} ${payment.ordenPagoId || ""} ${payment.reservaId || ""} ${payment.metodoPago || ""} ${payment.codigoTransaccion || ""} ${payment.estado || ""}`.toLowerCase();
        return searchable.includes(text) && (!statusFilter || payment.estado === statusFilter);
    });
}

function filteredOrders() {
    const text = searchText();
    const statusFilter = document.getElementById("filtroEstadoPagos")?.value || "";
    return state.orders.filter(order => {
        const searchable = `${order.ordenPagoId || ""} ${order.codigoOrden || ""} ${order.reservaId || ""} ${order.estado || ""}`.toLowerCase();
        return searchable.includes(text) && (!statusFilter || order.estado === statusFilter);
    });
}

async function loadReservationContext() {
    state.reservations.clear();
    if (!can(state.user, "VENTAS_RESERVAS_LEER")) return;
    const ids = [...new Set([...state.payments, ...state.orders].map(item => item.reservaId).filter(Boolean))];
    await Promise.all(ids.map(async id => {
        try {
            const detail = await api(`/api/reservas/${encodeURIComponent(id)}`);
            state.reservations.set(String(id), detail);
        } catch {
            state.reservations.set(String(id), null);
        }
    }));
}

function reservationClient(reservaId) {
    const detail = state.reservations.get(String(reservaId));
    if (!detail) return '<span class="admin-muted">No disponible</span>';
    return `<div class="admin-row-title"><strong>${escapeHtml(detail.cliente || "Cliente")}</strong><span>${escapeHtml(detail.codigoReserva || "")}</span></div>`;
}

function showPaymentDetail(id) {
    const payment = state.payments.find(item => String(item.pagoId) === String(id));
    if (!payment) return;
    const reservation = state.reservations.get(String(payment.reservaId));
    const html = `
        <div class="admin-detail-list">
            <p><strong>Pago ID:</strong> ${escapeHtml(payment.pagoId)}</p>
            <p><strong>Orden:</strong> ${escapeHtml(payment.ordenPagoId)}</p>
            <p><strong>Reserva:</strong> ${escapeHtml(payment.reservaId)}</p>
            <p><strong>Cliente:</strong> ${escapeHtml(reservation?.cliente || "No disponible")}</p>
            <p><strong>Método:</strong> ${escapeHtml(payment.metodoPago || "No disponible")}</p>
            <p><strong>Monto:</strong> ${money(payment.monto, payment.moneda)}</p>
            <p><strong>Estado:</strong> ${escapeHtml(payment.estado)}</p>
            <p><strong>Transacción:</strong> ${escapeHtml(payment.codigoTransaccion || "Sin transacción")}</p>
            <p><strong>Pasarela:</strong> ${escapeHtml(payment.referenciaPasarela || "No disponible")}</p>
            <p><strong>Observaciones:</strong> ${escapeHtml(payment.observaciones || "Sin observaciones")}</p>
        </div>`;
    modal("Detalle de pago", html);
}

async function changePaymentState(id) {
    const result = await Swal.fire({
        title: "Cambiar estado de pago",
        html: `
            <select id="adminPagoEstado" class="swal2-input">
                <option value="PENDIENTE">PENDIENTE</option>
                <option value="APROBADO">APROBADO</option>
                <option value="RECHAZADO">RECHAZADO</option>
                <option value="ERROR">ERROR</option>
            </select>
            <textarea id="adminPagoObservaciones" class="swal2-textarea" placeholder="Observaciones administrativas"></textarea>`,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: () => ({
            codigo: document.getElementById("adminPagoEstado").value,
            observaciones: document.getElementById("adminPagoObservaciones").value || null
        })
    });
    if (!result.isConfirmed) return;
    try {
        await api(`/api/pagos/admin/pagos/${encodeURIComponent(id)}/estado`, { method: "PUT", body: result.value });
        toast("Estado de pago actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo cambiar el estado del pago.", "error");
    }
}

function searchText() {
    return document.getElementById("buscarPagos")?.value.trim().toLowerCase() || "";
}

function modal(title, html) {
    if (window.Swal) Swal.fire({ title, html, width: 720, confirmButtonColor: "#f97316" });
    else alert(html.replace(/<[^>]+>/g, "\n"));
}

function toast(message, icon = "success") {
    if (window.Swal) Swal.fire({ text: message, icon, timer: 1800, showConfirmButton: false });
    else alert(message);
}

function formatDate(value) {
    if (!value) return "-";
    return new Date(value).toLocaleDateString("es-PE", { day: "2-digit", month: "short", year: "numeric" });
}

function money(value, currency = "PEN") {
    return new Intl.NumberFormat("es-PE", { style: "currency", currency: currency || "PEN" }).format(Number(value || 0));
}

function status(message) {
    const element = document.getElementById("pagosStatus");
    if (element) element.textContent = message;
}
