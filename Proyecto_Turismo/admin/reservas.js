import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = { reservations: [], page: 1, pageSize: 100, total: 0, user: null };

document.addEventListener("DOMContentLoaded", init);

async function init() {
    try {
        state.user = await initAdminPage({ active: "Reservas", title: "Reservas" });
    } catch {
        return;
    }
    if (!can(state.user, "VENTAS_RESERVAS_LEER")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.getElementById("buscarReservas")?.addEventListener("input", render);
    document.getElementById("filtroEstadoReservas")?.addEventListener("change", render);
    document.getElementById("filtroDesdeReservas")?.addEventListener("change", loadData);
    document.getElementById("filtroHastaReservas")?.addEventListener("change", loadData);
    document.getElementById("recargarReservas")?.addEventListener("click", loadData);
}

async function loadData() {
    const host = document.getElementById("reservasTableHost");
    host.innerHTML = adminLoading("Cargando reservas...");
    try {
        const params = new URLSearchParams({ page: state.page, pageSize: state.pageSize });
        const desde = document.getElementById("filtroDesdeReservas")?.value;
        const hasta = document.getElementById("filtroHastaReservas")?.value;
        if (desde) params.set("desde", desde);
        if (hasta) params.set("hasta", hasta);
        const page = await api(`/api/reservas?${params}`);
        state.reservations = page.items || [];
        state.total = page.total || state.reservations.length;
        render();
        status(`Mostrando ${state.reservations.length} de ${state.total} reservas.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudieron cargar las reservas.");
        status("No se pudieron cargar las reservas.");
    }
}

function render() {
    const host = document.getElementById("reservasTableHost");
    const rows = filtered();
    if (!rows.length) {
        host.innerHTML = adminEmpty("Sin reservas", "No hay reservas que coincidan con los filtros actuales.");
        return;
    }
    host.innerHTML = `
        <div class="admin-table-card">
            <div class="admin-table-meta">
                <span>${rows.length} reservas visibles</span>
                <span>Total registrado: ${state.total}</span>
            </div>
            <div class="wp-table-wrap">
                <table class="wp-table">
                    <thead>
                        <tr>
                            <th>Código</th>
                            <th>Cliente</th>
                            <th>Destino / paquete</th>
                            <th>Fecha</th>
                            <th>Estado</th>
                            <th>Total</th>
                            <th>Creación</th>
                            <th style="text-align:right;">Acciones</th>
                        </tr>
                    </thead>
                    <tbody>${rows.map(rowHtml).join("")}</tbody>
                </table>
            </div>
        </div>`;
    host.querySelectorAll("[data-action='detail']").forEach(button => button.addEventListener("click", () => showDetail(button.dataset.id)));
    host.querySelectorAll("[data-action='state']").forEach(button => button.addEventListener("click", () => changeState(button.dataset.id)));
}

function rowHtml(reserva) {
    const manage = can(state.user, "VENTAS_RESERVAS_GESTIONAR");
    return `
        <tr>
            <td><strong>${escapeHtml(reserva.codigoReserva)}</strong></td>
            <td><div class="admin-row-title"><strong>${escapeHtml(reserva.cliente || "Cliente")}</strong><span>ID ${escapeHtml(reserva.clienteId)}</span></div></td>
            <td><div class="admin-row-title"><strong>${escapeHtml(reserva.destino || "Destino")}</strong><span>${escapeHtml(reserva.paquete || "Paquete no disponible")}</span></div></td>
            <td>${formatRange(reserva.fechaInicio, reserva.fechaFin)}</td>
            <td>${stateBadge(reserva.estado)}</td>
            <td>${money(reserva.total, reserva.moneda)}</td>
            <td>${formatDate(reserva.fechaReserva)}</td>
            <td>
                <div class="admin-actions-cell">
                    <button class="admin-action-btn" data-action="detail" data-id="${escapeHtml(reserva.reservaId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                    ${manage ? `<button class="admin-action-btn" data-action="state" data-id="${escapeHtml(reserva.reservaId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>` : ""}
                </div>
            </td>
        </tr>`;
}

function filtered() {
    const text = document.getElementById("buscarReservas")?.value.trim().toLowerCase() || "";
    const statusFilter = document.getElementById("filtroEstadoReservas")?.value || "";
    return state.reservations.filter(reserva => {
        const searchable = `${reserva.codigoReserva || ""} ${reserva.cliente || ""} ${reserva.destino || ""} ${reserva.paquete || ""} ${reserva.estado || ""}`.toLowerCase();
        return searchable.includes(text) && (!statusFilter || reserva.estado === statusFilter);
    });
}

async function showDetail(id) {
    try {
        const detail = await api(`/api/reservas/${encodeURIComponent(id)}`);
        const html = `
            <div class="admin-detail-list">
                <p><strong>Código:</strong> ${escapeHtml(detail.codigoReserva)}</p>
                <p><strong>Cliente:</strong> ${escapeHtml(detail.cliente)}</p>
                <p><strong>Viaje:</strong> ${escapeHtml(detail.destino)} - ${escapeHtml(detail.paquete)}</p>
                <p><strong>Fechas:</strong> ${formatRange(detail.fechaInicio, detail.fechaFin)}</p>
                <p><strong>Total:</strong> ${money(detail.total, detail.moneda)}</p>
                <p><strong>Pasajeros:</strong> ${passengers(detail.pasajeros)}</p>
                <p><strong>Historial:</strong> ${history(detail.historial)}</p>
                <p><strong>Observaciones:</strong> ${escapeHtml(detail.observaciones || "Sin observaciones")}</p>
            </div>`;
        modal("Detalle de reserva", html);
    } catch (error) {
        toast(error.message || "No se pudo cargar el detalle.", "error");
    }
}

async function changeState(id) {
    const result = await Swal.fire({
        title: "Cambiar estado",
        html: `
            <select id="adminReservaEstado" class="swal2-input">
                <option value="PENDIENTE_PAGO">PENDIENTE_PAGO</option>
                <option value="CONFIRMADA">CONFIRMADA</option>
                <option value="CANCELADA">CANCELADA</option>
                <option value="COMPLETADA">COMPLETADA</option>
            </select>
            <textarea id="adminReservaComentario" class="swal2-textarea" placeholder="Comentario administrativo"></textarea>`,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: () => ({
            codigo: document.getElementById("adminReservaEstado").value,
            comentario: document.getElementById("adminReservaComentario").value || null
        })
    });
    if (!result.isConfirmed) return;
    try {
        await api(`/api/reservas/${encodeURIComponent(id)}/estado`, { method: "PUT", body: result.value });
        toast("Estado de reserva actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo cambiar el estado.", "error");
    }
}

function passengers(items = []) {
    if (!items.length) return "Sin pasajeros";
    return items.map(item => `${escapeHtml(item.nombres)} ${escapeHtml(item.apellidos)} (${escapeHtml(item.tipoPasajero)})`).join("<br>");
}

function history(items = []) {
    if (!items.length) return "Sin historial";
    return items.map(item => `${escapeHtml(item.estadoAnterior || "Inicio")} -> ${escapeHtml(item.estadoNuevo)} · ${formatDate(item.creadoEn)}`).join("<br>");
}

function modal(title, html) {
    if (window.Swal) Swal.fire({ title, html, width: 760, confirmButtonColor: "#f97316" });
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

function formatRange(start, end) {
    return `${formatDate(start)} - ${formatDate(end)}`;
}

function money(value, currency = "PEN") {
    return new Intl.NumberFormat("es-PE", { style: "currency", currency: currency || "PEN" }).format(Number(value || 0));
}

function status(message) {
    const element = document.getElementById("reservasStatus");
    if (element) element.textContent = message;
}
