import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = { clients: [], page: 1, pageSize: 100, total: 0 };

document.addEventListener("DOMContentLoaded", init);

async function init() {
    let user;
    try {
        user = await initAdminPage({ active: "Clientes", title: "Clientes" });
    } catch {
        return;
    }
    if (!can(user, "SEGURIDAD_USUARIOS_GESTIONAR")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.getElementById("buscarClientes")?.addEventListener("input", render);
    document.getElementById("filtroEstadoClientes")?.addEventListener("change", render);
    document.getElementById("recargarClientes")?.addEventListener("click", loadData);
}

async function loadData() {
    const host = document.getElementById("clientesTableHost");
    host.innerHTML = adminLoading("Cargando clientes...");
    try {
        const page = await api(`/api/clientes?page=${state.page}&pageSize=${state.pageSize}`);
        state.clients = page.items || [];
        state.total = page.total || state.clients.length;
        render();
        status(`Mostrando ${state.clients.length} de ${state.total} clientes.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudieron cargar los clientes.");
        status("No se pudieron cargar los clientes.");
    }
}

function render() {
    const host = document.getElementById("clientesTableHost");
    const rows = filtered();
    if (!rows.length) {
        host.innerHTML = adminEmpty("Sin clientes", "No hay clientes que coincidan con los filtros actuales.");
        return;
    }
    host.innerHTML = `
        <div class="admin-table-card">
            <div class="admin-table-meta">
                <span>${rows.length} clientes visibles</span>
                <span>Total registrado: ${state.total}</span>
            </div>
            <div class="wp-table-wrap">
                <table class="wp-table">
                    <thead>
                        <tr>
                            <th>Cliente</th>
                            <th>Estado</th>
                            <th>Contacto</th>
                            <th>Usuario asociado</th>
                            <th>Creación</th>
                            <th style="text-align:right;">Acciones</th>
                        </tr>
                    </thead>
                    <tbody>${rows.map(rowHtml).join("")}</tbody>
                </table>
            </div>
        </div>`;
    host.querySelectorAll("[data-action='state']").forEach(button => button.addEventListener("click", () => changeState(button.dataset.id, button.dataset.next)));
    host.querySelectorAll("[data-action='detail']").forEach(button => button.addEventListener("click", () => showDetail(button.dataset.id)));
}

function rowHtml(client) {
    const active = String(client.estado || "").toUpperCase() === "ACTIVO";
    return `
        <tr>
            <td><div class="admin-row-title"><strong>${escapeHtml(`${client.nombres || ""} ${client.apellidos || ""}`.trim() || "Sin nombre")}</strong><span>ID ${escapeHtml(client.clienteId)}</span></div></td>
            <td>${stateBadge(client.estado)}</td>
            <td><div class="admin-row-title"><strong>${escapeHtml(client.telefono || "Sin teléfono")}</strong><span>${escapeHtml(client.email || "Sin email")}</span></div></td>
            <td><code>${escapeHtml(client.usuarioId)}</code></td>
            <td>${formatDate(client.creadoEn)}</td>
            <td>
                <div class="admin-actions-cell">
                    <button class="admin-action-btn" data-action="detail" data-id="${escapeHtml(client.clienteId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                    <button class="admin-action-btn ${active ? "danger" : ""}" data-action="state" data-id="${escapeHtml(client.clienteId)}" data-next="${active ? "INACTIVO" : "ACTIVO"}">
                        <i class="fa-solid ${active ? "fa-ban" : "fa-check"}"></i> ${active ? "Desactivar" : "Activar"}
                    </button>
                </div>
            </td>
        </tr>`;
}

function filtered() {
    const text = document.getElementById("buscarClientes")?.value.trim().toLowerCase() || "";
    const statusFilter = document.getElementById("filtroEstadoClientes")?.value || "";
    return state.clients.filter(client => {
        const searchable = `${client.nombres || ""} ${client.apellidos || ""} ${client.email || ""} ${client.telefono || ""} ${client.estado || ""}`.toLowerCase();
        return searchable.includes(text) && (!statusFilter || client.estado === statusFilter);
    });
}

async function changeState(id, next) {
    const result = await confirm("Cambiar estado", `Se cambiará el estado del cliente a ${next}.`);
    if (!result) return;
    try {
        await api(`/api/clientes/${encodeURIComponent(id)}/state`, { method: "PUT", body: { codigo: next } });
        toast("Cliente actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo actualizar el cliente.", "error");
    }
}

function showDetail(id) {
    const client = state.clients.find(item => String(item.clienteId) === String(id));
    if (!client) return;
    const html = `
        <div class="admin-detail-list">
            <p><strong>Cliente:</strong> ${escapeHtml(`${client.nombres} ${client.apellidos}`)}</p>
            <p><strong>Email:</strong> ${escapeHtml(client.email)}</p>
            <p><strong>Teléfono:</strong> ${escapeHtml(client.telefono || "No indicado")}</p>
            <p><strong>Estado:</strong> ${escapeHtml(client.estado)}</p>
            <p><strong>Usuario ID:</strong> ${escapeHtml(client.usuarioId)}</p>
            <p><strong>Creado:</strong> ${formatDate(client.creadoEn)}</p>
        </div>`;
    if (window.Swal) Swal.fire({ title: "Detalle de cliente", html, confirmButtonColor: "#f97316" });
    else alert(html.replace(/<[^>]+>/g, "\n"));
}

async function confirm(title, text) {
    if (!window.Swal) return window.confirm(`${title}\n${text}`);
    const result = await Swal.fire({ title, text, icon: "question", showCancelButton: true, confirmButtonText: "Confirmar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316" });
    return result.isConfirmed;
}

function toast(message, icon = "success") {
    if (window.Swal) Swal.fire({ text: message, icon, timer: 1800, showConfirmButton: false });
    else alert(message);
}

function formatDate(value) {
    if (!value) return "-";
    return new Date(value).toLocaleDateString("es-PE", { day: "2-digit", month: "short", year: "numeric" });
}

function status(message) {
    const element = document.getElementById("clientesStatus");
    if (element) element.textContent = message;
}
