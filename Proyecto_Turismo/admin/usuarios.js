import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = { users: [], roles: [], page: 1, pageSize: 100, total: 0 };

document.addEventListener("DOMContentLoaded", init);

async function init() {
    let user;
    try {
        user = await initAdminPage({ active: "Usuarios", title: "Usuarios" });
    } catch {
        return;
    }
    if (!can(user, "SEGURIDAD_USUARIOS_GESTIONAR")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.getElementById("buscarUsuarios")?.addEventListener("input", render);
    document.getElementById("filtroEstadoUsuarios")?.addEventListener("change", render);
    document.getElementById("recargarUsuarios")?.addEventListener("click", loadData);
}

async function loadData() {
    const host = document.getElementById("usuariosTableHost");
    host.innerHTML = adminLoading("Cargando usuarios...");
    try {
        const [usersPage, roles] = await Promise.all([
            api(`/api/security/users?page=${state.page}&pageSize=${state.pageSize}`),
            api("/api/security/roles")
        ]);
        state.users = usersPage.items || [];
        state.total = usersPage.total || state.users.length;
        state.roles = roles || [];
        render();
        status(`Mostrando ${state.users.length} de ${state.total} usuarios.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudieron cargar los usuarios.");
        status("No se pudieron cargar los usuarios.");
    }
}

function render() {
    const host = document.getElementById("usuariosTableHost");
    const rows = filtered();
    if (!rows.length) {
        host.innerHTML = adminEmpty("Sin usuarios", "No hay usuarios que coincidan con los filtros actuales.");
        return;
    }
    host.innerHTML = `
        <div class="admin-table-card">
            <div class="admin-table-meta">
                <span>${rows.length} usuarios visibles</span>
                <span>Roles disponibles: ${state.roles.length}</span>
            </div>
            <div class="wp-table-wrap">
                <table class="wp-table">
                    <thead>
                        <tr>
                            <th>Usuario</th>
                            <th>Estado</th>
                            <th>Email</th>
                            <th>Roles</th>
                            <th>Versión</th>
                            <th style="text-align:right;">Acciones</th>
                        </tr>
                    </thead>
                    <tbody>${rows.map(rowHtml).join("")}</tbody>
                </table>
            </div>
        </div>`;
    host.querySelectorAll("[data-action='state']").forEach(button => button.addEventListener("click", () => changeState(button.dataset.id, button.dataset.next)));
    host.querySelectorAll("[data-action='roles']").forEach(button => button.addEventListener("click", () => changeRoles(button.dataset.id)));
}

function rowHtml(user) {
    const id = user.usuarioId;
    const active = String(user.estado || "").toUpperCase() === "ACTIVO";
    return `
        <tr>
            <td><div class="admin-row-title"><strong>${escapeHtml(user.nombre || "Sin nombre")}</strong><span>ID ${escapeHtml(id)}</span></div></td>
            <td>${stateBadge(user.estado)}</td>
            <td>${escapeHtml(user.email)}</td>
            <td>${roleSummary(user)}</td>
            <td>${escapeHtml(user.version || "-")}</td>
            <td>
                <div class="admin-actions-cell">
                    <button class="admin-action-btn" data-action="roles" data-id="${escapeHtml(id)}"><i class="fa-solid fa-user-tag"></i> Roles</button>
                    <button class="admin-action-btn ${active ? "danger" : ""}" data-action="state" data-id="${escapeHtml(id)}" data-next="${active ? "BLOQUEADO" : "ACTIVO"}">
                        <i class="fa-solid ${active ? "fa-ban" : "fa-check"}"></i> ${active ? "Bloquear" : "Activar"}
                    </button>
                </div>
            </td>
        </tr>`;
}

function filtered() {
    const text = document.getElementById("buscarUsuarios")?.value.trim().toLowerCase() || "";
    const statusFilter = document.getElementById("filtroEstadoUsuarios")?.value || "";
    return state.users.filter(user => {
        const searchable = `${user.nombre || ""} ${user.email || ""} ${user.estado || ""}`.toLowerCase();
        return searchable.includes(text) && (!statusFilter || user.estado === statusFilter);
    });
}

function roleSummary(user) {
    const direct = user.roles || user.rol || user.role;
    if (Array.isArray(direct)) return direct.map(escapeHtml).join(", ");
    if (direct) return escapeHtml(direct);
    return '<span class="admin-muted">Gestionar para consultar/asignar</span>';
}

async function changeState(id, next) {
    const result = await confirm("Cambiar estado", `Se cambiará el estado del usuario a ${next}.`);
    if (!result) return;
    try {
        await api(`/api/security/users/${encodeURIComponent(id)}/state`, { method: "PUT", body: { codigo: next } });
        toast("Estado actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo actualizar el estado.", "error");
    }
}

async function changeRoles(id) {
    const options = state.roles.map(role => `<option value="${role.rolId}">${escapeHtml(role.codigo)} - ${escapeHtml(role.nombre)}</option>`).join("");
    const result = await Swal.fire({
        title: "Asignar rol",
        html: `<select id="adminRoleSelect" class="swal2-input">${options}</select>`,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: () => Number(document.getElementById("adminRoleSelect").value)
    });
    if (!result.isConfirmed) return;
    try {
        await api(`/api/security/users/${encodeURIComponent(id)}/roles`, { method: "PUT", body: { rolIds: [result.value] } });
        toast("Rol actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo actualizar el rol.", "error");
    }
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

function status(message) {
    const element = document.getElementById("usuariosStatus");
    if (element) element.textContent = message;
}
