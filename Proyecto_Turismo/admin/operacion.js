import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = {
    activeTab: "salidas",
    departures: [],
    availability: [],
    providers: [],
    services: [],
    resources: [],
    blocks: [],
    capacity: [],
    itineraries: [],
    assignments: [],
    packages: [],
    options: { estadosSalida: [], estadosProveedor: [], estadosRecurso: [], areas: [] },
    user: null
};

document.addEventListener("DOMContentLoaded", init);

async function init() {
    try {
        state.user = await initAdminPage({ active: "Operación", title: "Operación" });
    } catch {
        return;
    }
    if (!can(state.user, "VENTAS_RESERVAS_GESTIONAR")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.querySelectorAll(".admin-tab").forEach(button => {
        button.addEventListener("click", () => {
            state.activeTab = button.dataset.tab || "salidas";
            document.querySelectorAll(".admin-tab").forEach(tab => tab.classList.toggle("active", tab === button));
            document.getElementById("crearOperacion").hidden = state.activeTab === "recursos";
            render();
        });
    });
    document.getElementById("buscarOperacion")?.addEventListener("input", render);
    document.getElementById("filtroTipoOperacion")?.addEventListener("change", render);
    document.getElementById("filtroDesdeOperacion")?.addEventListener("change", loadData);
    document.getElementById("filtroHastaOperacion")?.addEventListener("change", loadData);
    document.getElementById("recargarOperacion")?.addEventListener("click", loadData);
    document.getElementById("crearOperacion")?.addEventListener("click", createForActiveTab);
}

async function loadData() {
    const host = document.getElementById("operacionTableHost");
    host.innerHTML = adminLoading("Cargando operación...");
    try {
        const dateParams = new URLSearchParams();
        const desde = document.getElementById("filtroDesdeOperacion")?.value;
        const hasta = document.getElementById("filtroHastaOperacion")?.value;
        if (desde) dateParams.set("desde", desde);
        if (hasta) dateParams.set("hasta", hasta);

        const [departures, availability, providers, services, resources, blocks, options, packages] = await Promise.all([
            api(`/api/operacion/salidas?${dateParams}`),
            api(`/api/operacion/disponibilidad?${dateParams}`),
            api("/api/operacion/proveedores"),
            api("/api/operacion/proveedor-servicios"),
            api("/api/operacion/recursos"),
            api("/api/operacion/bloqueos"),
            api("/api/operacion/opciones"),
            api("/api/catalogo/paquetes")
        ]);
        state.departures = departures || [];
        state.availability = availability || [];
        state.providers = providers || [];
        state.services = services || [];
        state.resources = resources || [];
        state.blocks = blocks || [];
        state.options = options || state.options;
        state.packages = packages || [];
        await loadNestedOperation();
        render();
        status(`Operación cargada: ${state.departures.length} salidas, ${state.providers.length} proveedores y ${state.resources.length} recursos.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudo cargar la operación.");
        status("No se pudo cargar la operación.");
    }
}

async function loadNestedOperation() {
    state.capacity = [];
    state.itineraries = [];
    state.assignments = [];
    await Promise.all(state.departures.map(async departure => {
        const salidaId = departure.salidaId;
        try {
            const [cupos, itinerario, asignaciones] = await Promise.all([
                api(`/api/operacion/salidas/${encodeURIComponent(salidaId)}/cupos`),
                api(`/api/operacion/salidas/${encodeURIComponent(salidaId)}/itinerario`),
                api(`/api/operacion/salidas/${encodeURIComponent(salidaId)}/asignaciones`)
            ]);
            state.capacity.push(...(cupos || []).map(item => ({ ...item, salidaId, paquete: departure.paquete })));
            state.itineraries.push(...(itinerario || []).map(item => ({ ...item, paquete: departure.paquete })));
            state.assignments.push(...(asignaciones || []).map(item => ({ ...item, paquete: departure.paquete })));
        } catch {
            state.capacity.push();
        }
    }));
}

function render() {
    const host = document.getElementById("operacionTableHost");
    const views = {
        salidas: renderDepartures,
        cupos: renderCapacity,
        proveedores: renderProviders,
        servicios: renderServices,
        recursos: renderResources,
        bloqueos: renderBlocks,
        itinerarios: renderItineraries,
        asignaciones: renderAssignments
    };
    host.innerHTML = views[state.activeTab]?.() || renderDepartures();
    bindTableActions(host);
}

function renderDepartures() {
    const rows = state.departures.filter(item => matchesText(`${item.paquete} ${item.estado} ${item.puntoEncuentro || ""}`) && matchesType(item.estado));
    if (!rows.length) return adminEmpty("Sin salidas", "No hay salidas que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} salidas visibles`, "API: /api/operacion/salidas", `
        <table class="wp-table"><thead><tr><th>Paquete</th><th>Fechas</th><th>Cupos</th><th>Encuentro</th><th>Estado</th><th style="text-align:right;">Acciones</th></tr></thead>
        <tbody>${rows.map(rowDeparture).join("")}</tbody></table>`);
}

function rowDeparture(item) {
    return `<tr>
        <td><div class="admin-row-title"><strong>${escapeHtml(item.paquete)}</strong><span>ID ${escapeHtml(item.salidaId)}</span></div></td>
        <td>${formatDate(item.fechaInicio)} - ${formatDate(item.fechaFin)}</td>
        <td>${escapeHtml(item.cupoDisponible)} disponibles / ${escapeHtml(item.cupoTotal)} total</td>
        <td>${escapeHtml(item.puntoEncuentro || "-")}</td>
        <td>${stateBadge(item.estado)}</td>
        <td><div class="admin-actions-cell">
            <button class="admin-action-btn" data-action="salida-edit" data-id="${escapeHtml(item.salidaId)}"><i class="fa-solid fa-pen"></i> Editar</button>
            <button class="admin-action-btn" data-action="salida-state" data-id="${escapeHtml(item.salidaId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>
        </div></td>
    </tr>`;
}

function renderCapacity() {
    const rows = state.capacity.filter(item => matchesText(`${item.paquete} ${item.tipoCupo}`));
    if (!rows.length) return adminEmpty("Sin cupos", "No hay cupos registrados para las salidas cargadas.");
    return tableCard(`${rows.length} cupos visibles`, "API: /api/operacion/salidas/{id}/cupos", `
        <table class="wp-table"><thead><tr><th>Salida</th><th>Tipo cupo</th><th>Total</th><th>Reservado</th><th>Disponible</th><th style="text-align:right;">Acciones</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${escapeHtml(item.paquete)}<br><span class="admin-muted">Salida ${escapeHtml(item.salidaId)}</span></td><td>${escapeHtml(item.tipoCupo)}</td><td>${escapeHtml(item.cupoTotal)}</td><td>${escapeHtml(item.cupoReservado)}</td><td>${escapeHtml(item.cupoDisponible)}</td><td><div class="admin-actions-cell"><button class="admin-action-btn" data-action="cupo-edit" data-id="${escapeHtml(item.salidaId)}" data-type="${escapeHtml(item.tipoCupo)}"><i class="fa-solid fa-pen"></i> Editar</button></div></td></tr>`).join("")}</tbody></table>`);
}

function renderProviders() {
    const rows = state.providers.filter(item => matchesText(`${item.razonSocial} ${item.nombreComercial || ""} ${item.email || ""} ${item.ciudad || ""} ${item.estado}`) && matchesType(item.estado));
    if (!rows.length) return adminEmpty("Sin proveedores", "No hay proveedores que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} proveedores visibles`, "API: /api/operacion/proveedores", `
        <table class="wp-table"><thead><tr><th>Proveedor</th><th>Contacto</th><th>Ciudad</th><th>Estado</th><th style="text-align:right;">Acciones</th></tr></thead>
        <tbody>${rows.map(rowProvider).join("")}</tbody></table>`);
}

function rowProvider(item) {
    return `<tr>
        <td><div class="admin-row-title"><strong>${escapeHtml(item.razonSocial)}</strong><span>${escapeHtml(item.nombreComercial || "Sin nombre comercial")}</span></div></td>
        <td>${escapeHtml(item.email || "-")}<br><span class="admin-muted">${escapeHtml(item.telefono || "-")}</span></td>
        <td>${escapeHtml(item.ciudad || "-")}</td>
        <td>${stateBadge(item.estado)}</td>
        <td><div class="admin-actions-cell"><button class="admin-action-btn" data-action="proveedor-edit" data-id="${escapeHtml(item.proveedorId)}"><i class="fa-solid fa-pen"></i> Editar</button><button class="admin-action-btn" data-action="proveedor-state" data-id="${escapeHtml(item.proveedorId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button></div></td>
    </tr>`;
}

function renderServices() {
    const rows = state.services.filter(item => matchesText(`${item.proveedor} ${item.tipoServicio} ${item.nombre} ${item.estado}`) && matchesType(item.tipoServicio, item.estado));
    if (!rows.length) return adminEmpty("Sin servicios", "No hay servicios de proveedor que coincidan con los filtros.");
    return tableCard(`${rows.length} servicios visibles`, "API: /api/operacion/proveedor-servicios", `
        <table class="wp-table"><thead><tr><th>Servicio</th><th>Proveedor</th><th>Tipo</th><th>Costo</th><th>Estado</th><th style="text-align:right;">Acciones</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td><strong>${escapeHtml(item.nombre)}</strong><br><span class="admin-muted">${escapeHtml(item.descripcion || "-")}</span></td><td>${escapeHtml(item.proveedor)}</td><td>${escapeHtml(item.tipoServicio)}</td><td>${money(item.costoBase, item.moneda)}</td><td>${stateBadge(item.estado)}</td><td><div class="admin-actions-cell"><button class="admin-action-btn" data-action="servicio-edit" data-id="${escapeHtml(item.proveedorServicioId)}"><i class="fa-solid fa-pen"></i> Editar</button></div></td></tr>`).join("")}</tbody></table>`);
}

function renderResources() {
    const rows = state.resources.filter(item => matchesText(`${item.tipo} ${item.nombre} ${item.detalle || ""} ${item.estado}`) && matchesType(item.tipo, item.estado));
    if (!rows.length) return adminEmpty("Sin recursos", "No hay hoteles, transporte o guías que coincidan con los filtros.");
    return tableCard(`${rows.length} recursos visibles`, "API: /api/operacion/recursos", `
        <table class="wp-table"><thead><tr><th>Tipo</th><th>Nombre</th><th>Detalle</th><th>Capacidad</th><th>Costo</th><th>Estado</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${escapeHtml(item.tipo)}</td><td><strong>${escapeHtml(item.nombre)}</strong><br><span class="admin-muted">ID ${escapeHtml(item.id)}</span></td><td>${escapeHtml(item.detalle || "-")}</td><td>${escapeHtml(item.capacidad ?? "-")}</td><td>${item.costo == null ? "-" : money(item.costo)}</td><td>${stateBadge(item.estado)}</td></tr>`).join("")}</tbody></table>`);
}

function renderBlocks() {
    const rows = state.blocks.filter(item => matchesText(`${item.motivo} ${item.paqueteId || ""} ${item.salidaId || ""}`));
    if (!rows.length) return adminEmpty("Sin bloqueos", "No hay bloqueos de disponibilidad registrados.");
    return tableCard(`${rows.length} bloqueos visibles`, "API: /api/operacion/bloqueos", `
        <table class="wp-table"><thead><tr><th>Rango</th><th>Paquete</th><th>Salida</th><th>Motivo</th><th>Creado</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${formatDate(item.fechaInicio)} - ${formatDate(item.fechaFin)}</td><td>${escapeHtml(item.paqueteId || "-")}</td><td>${escapeHtml(item.salidaId || "-")}</td><td>${escapeHtml(item.motivo)}</td><td>${formatDate(item.creadoEn)}</td></tr>`).join("")}</tbody></table>`);
}

function renderItineraries() {
    const rows = state.itineraries.filter(item => matchesText(`${item.paquete} ${item.actividad} ${item.descripcion || ""}`));
    if (!rows.length) return adminEmpty("Sin itinerarios", "No hay itinerarios operativos para las salidas cargadas.");
    return tableCard(`${rows.length} actividades visibles`, "API: /api/operacion/salidas/{id}/itinerario", `
        <table class="wp-table"><thead><tr><th>Salida</th><th>Día</th><th>Hora</th><th>Actividad</th><th>Descripción</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${escapeHtml(item.paquete)}<br><span class="admin-muted">Salida ${escapeHtml(item.salidaId)}</span></td><td>${escapeHtml(item.diaNumero)}</td><td>${escapeHtml(item.horaInicio || "-")} - ${escapeHtml(item.horaFin || "-")}</td><td><strong>${escapeHtml(item.actividad)}</strong></td><td>${escapeHtml(item.descripcion || "-")}</td></tr>`).join("")}</tbody></table>`);
}

function renderAssignments() {
    const rows = state.assignments.filter(item => matchesText(`${item.paquete} ${item.proveedorServicio || ""} ${item.guia || ""} ${item.transporte || ""} ${item.hotel || ""}`));
    if (!rows.length) return adminEmpty("Sin asignaciones", "No hay recursos asignados a las salidas cargadas.");
    return tableCard(`${rows.length} asignaciones visibles`, "API: /api/operacion/salidas/{id}/asignaciones", `
        <table class="wp-table"><thead><tr><th>Salida</th><th>Servicio</th><th>Guía</th><th>Transporte</th><th>Hotel</th><th>Observaciones</th></tr></thead>
        <tbody>${rows.map(item => `<tr><td>${escapeHtml(item.paquete)}<br><span class="admin-muted">Salida ${escapeHtml(item.salidaId)}</span></td><td>${escapeHtml(item.proveedorServicio || "-")}</td><td>${escapeHtml(item.guia || "-")}</td><td>${escapeHtml(item.transporte || "-")}</td><td>${escapeHtml(item.hotel || "-")}</td><td>${escapeHtml(item.observaciones || "-")}</td></tr>`).join("")}</tbody></table>`);
}

function bindTableActions(host) {
    host.querySelectorAll("[data-action='salida-edit']").forEach(button => button.addEventListener("click", () => upsertDeparture(button.dataset.id)));
    host.querySelectorAll("[data-action='salida-state']").forEach(button => button.addEventListener("click", () => changeState("salidas", button.dataset.id)));
    host.querySelectorAll("[data-action='cupo-edit']").forEach(button => button.addEventListener("click", () => upsertCapacity(button.dataset.id, button.dataset.type)));
    host.querySelectorAll("[data-action='proveedor-edit']").forEach(button => button.addEventListener("click", () => upsertProvider(button.dataset.id)));
    host.querySelectorAll("[data-action='proveedor-state']").forEach(button => button.addEventListener("click", () => changeState("proveedores", button.dataset.id)));
    host.querySelectorAll("[data-action='servicio-edit']").forEach(button => button.addEventListener("click", () => upsertService(button.dataset.id)));
}

function createForActiveTab() {
    const actions = {
        salidas: () => upsertDeparture(),
        cupos: () => upsertCapacity(),
        proveedores: () => upsertProvider(),
        servicios: () => upsertService(),
        bloqueos: () => createBlock(),
        itinerarios: () => createItinerary(),
        asignaciones: () => createAssignment()
    };
    actions[state.activeTab]?.();
}

async function upsertDeparture(id) {
    const current = state.departures.find(item => String(item.salidaId) === String(id));
    const result = await Swal.fire({ title: id ? "Editar salida" : "Nueva salida", html: departureForm(current), width: 720, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readDepartureForm });
    if (!result.isConfirmed) return;
    await save(`/api/operacion/salidas${id ? `/${encodeURIComponent(id)}` : ""}`, id ? "PUT" : "POST", result.value, "Salida guardada.");
}

async function upsertCapacity(salidaId, tipoCupo) {
    const current = state.capacity.find(item => String(item.salidaId) === String(salidaId) && (!tipoCupo || item.tipoCupo === tipoCupo));
    const result = await Swal.fire({ title: "Gestionar cupo", html: capacityForm(current, salidaId), showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readCapacityForm });
    if (!result.isConfirmed) return;
    await save(`/api/operacion/salidas/${encodeURIComponent(result.value.salidaId)}/cupos`, "PUT", result.value.body, "Cupo guardado.");
}

async function upsertProvider(id) {
    const current = state.providers.find(item => String(item.proveedorId) === String(id));
    const result = await Swal.fire({ title: id ? "Editar proveedor" : "Nuevo proveedor", html: providerForm(current), width: 720, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readProviderForm });
    if (!result.isConfirmed) return;
    await save(`/api/operacion/proveedores${id ? `/${encodeURIComponent(id)}` : ""}`, id ? "PUT" : "POST", result.value, "Proveedor guardado.");
}

async function upsertService(id) {
    const current = state.services.find(item => String(item.proveedorServicioId) === String(id));
    const result = await Swal.fire({ title: id ? "Editar servicio" : "Nuevo servicio", html: serviceForm(current), width: 720, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readServiceForm });
    if (!result.isConfirmed) return;
    await save(`/api/operacion/proveedor-servicios${id ? `/${encodeURIComponent(id)}` : ""}`, id ? "PUT" : "POST", result.value, "Servicio guardado.");
}

async function createBlock() {
    const result = await Swal.fire({ title: "Nuevo bloqueo", html: blockForm(), width: 720, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readBlockForm });
    if (!result.isConfirmed) return;
    await save("/api/operacion/bloqueos", "POST", result.value, "Bloqueo creado.");
}

async function createItinerary() {
    const result = await Swal.fire({ title: "Nueva actividad operativa", html: itineraryForm(), width: 720, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readItineraryForm });
    if (!result.isConfirmed) return;
    await save("/api/operacion/itinerario", "POST", result.value, "Itinerario creado.");
}

async function createAssignment() {
    const result = await Swal.fire({ title: "Nueva asignación", html: assignmentForm(), width: 760, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: readAssignmentForm });
    if (!result.isConfirmed) return;
    await save("/api/operacion/asignaciones", "POST", result.value, "Asignación creada.");
}

async function changeState(type, id) {
    const result = await Swal.fire({ title: "Cambiar estado", html: `<select id="operacionEstado" class="swal2-input"><option value="ACTIVO">ACTIVO</option><option value="INACTIVO">INACTIVO</option><option value="PROGRAMADA">PROGRAMADA</option><option value="CANCELADA">CANCELADA</option></select>`, showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar", confirmButtonColor: "#f97316", preConfirm: () => document.getElementById("operacionEstado").value });
    if (!result.isConfirmed) return;
    await save(`/api/operacion/${type}/${encodeURIComponent(id)}/state`, "PUT", { codigo: result.value }, "Estado actualizado.");
}

async function save(url, method, body, message) {
    try {
        await api(url, { method, body });
        toast(message);
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo completar la operación.", "error");
    }
}

function departureForm(item) {
    return `<div class="admin-form-grid">${select("salidaPaquete", "Paquete", state.packages, "paqueteId", "nombre", item?.paqueteId)}
        <input id="salidaInicio" class="swal2-input" type="date" value="${escapeHtml(item?.fechaInicio || "")}">
        <input id="salidaFin" class="swal2-input" type="date" value="${escapeHtml(item?.fechaFin || "")}">
        <input id="salidaCupoTotal" class="swal2-input" type="number" min="0" placeholder="Cupo total" value="${escapeHtml(item?.cupoTotal ?? 0)}">
        <input id="salidaCupoReservado" class="swal2-input" type="number" min="0" placeholder="Cupo reservado" value="${escapeHtml(item?.cupoReservado ?? 0)}">
        <input id="salidaPunto" class="swal2-input" placeholder="Punto de encuentro" value="${escapeHtml(item?.puntoEncuentro || "")}"></div>`;
}

function readDepartureForm() {
    return { paqueteId: Number(value("salidaPaquete")), fechaInicio: value("salidaInicio"), fechaFin: value("salidaFin"), cupoTotal: Number(value("salidaCupoTotal")), cupoReservado: Number(value("salidaCupoReservado")), puntoEncuentro: value("salidaPunto") || null };
}

function capacityForm(item, salidaId) {
    return `<div class="admin-form-grid">${select("cupoSalida", "Salida", state.departures, "salidaId", "paquete", salidaId || item?.salidaId)}
        <input id="cupoTipo" class="swal2-input" placeholder="Tipo cupo" value="${escapeHtml(item?.tipoCupo || "GENERAL")}">
        <input id="cupoTotal" class="swal2-input" type="number" min="0" placeholder="Cupo total" value="${escapeHtml(item?.cupoTotal ?? 0)}">
        <input id="cupoReservado" class="swal2-input" type="number" min="0" placeholder="Cupo reservado" value="${escapeHtml(item?.cupoReservado ?? 0)}"></div>`;
}

function readCapacityForm() {
    return { salidaId: Number(value("cupoSalida")), body: { tipoCupo: value("cupoTipo"), cupoTotal: Number(value("cupoTotal")), cupoReservado: Number(value("cupoReservado")) } };
}

function providerForm(item) {
    return `<div class="admin-form-grid"><input id="proveedorRazon" class="swal2-input" placeholder="Razón social" value="${escapeHtml(item?.razonSocial || "")}">
        <input id="proveedorComercial" class="swal2-input" placeholder="Nombre comercial" value="${escapeHtml(item?.nombreComercial || "")}">
        <input id="proveedorEmail" class="swal2-input" type="email" placeholder="Email" value="${escapeHtml(item?.email || "")}">
        <input id="proveedorTelefono" class="swal2-input" placeholder="Teléfono" value="${escapeHtml(item?.telefono || "")}"></div>`;
}

function readProviderForm() {
    return { tipoDocumentoId: null, numeroDocumento: null, razonSocial: value("proveedorRazon"), nombreComercial: value("proveedorComercial") || null, email: value("proveedorEmail") || null, telefono: value("proveedorTelefono") || null, ciudadId: null };
}

function serviceForm(item) {
    return `<div class="admin-form-grid">${select("servicioProveedor", "Proveedor", state.providers, "proveedorId", "razonSocial", item?.proveedorId)}
        <select id="servicioTipo" class="swal2-input">${["HOTEL", "TRANSPORTE", "GUIA", "ENTRADA", "ALIMENTACION", "SEGURO", "OTRO"].map(type => `<option value="${type}" ${item?.tipoServicio === type ? "selected" : ""}>${type}</option>`).join("")}</select>
        <input id="servicioNombre" class="swal2-input" placeholder="Nombre" value="${escapeHtml(item?.nombre || "")}">
        <textarea id="servicioDescripcion" class="swal2-textarea" placeholder="Descripción">${escapeHtml(item?.descripcion || "")}</textarea>
        <input id="servicioCosto" class="swal2-input" type="number" min="0" step="0.01" placeholder="Costo base" value="${escapeHtml(item?.costoBase ?? 0)}"></div>`;
}

function readServiceForm() {
    return { proveedorId: Number(value("servicioProveedor")), monedaId: 1, tipoServicio: value("servicioTipo"), nombre: value("servicioNombre"), descripcion: value("servicioDescripcion") || null, costoBase: Number(value("servicioCosto")) };
}

function blockForm() {
    return `<div class="admin-form-grid">${select("bloqueoSalida", "Salida", [{ salidaId: "", paquete: "Sin salida específica" }, ...state.departures], "salidaId", "paquete", "")}
        <input id="bloqueoInicio" class="swal2-input" type="date"><input id="bloqueoFin" class="swal2-input" type="date">
        <input id="bloqueoMotivo" class="swal2-input" placeholder="Motivo"></div>`;
}

function readBlockForm() {
    const salidaId = value("bloqueoSalida");
    return { paqueteId: null, salidaId: salidaId ? Number(salidaId) : null, fechaInicio: value("bloqueoInicio"), fechaFin: value("bloqueoFin"), motivo: value("bloqueoMotivo") };
}

function itineraryForm() {
    return `<div class="admin-form-grid">${select("itinerarioSalida", "Salida", state.departures, "salidaId", "paquete", "")}
        <input id="itinerarioDia" class="swal2-input" type="number" min="1" placeholder="Día">
        <input id="itinerarioHoraInicio" class="swal2-input" type="time"><input id="itinerarioHoraFin" class="swal2-input" type="time">
        <input id="itinerarioActividad" class="swal2-input" placeholder="Actividad"><textarea id="itinerarioDescripcion" class="swal2-textarea" placeholder="Descripción"></textarea></div>`;
}

function readItineraryForm() {
    return { salidaId: Number(value("itinerarioSalida")), diaNumero: Number(value("itinerarioDia")), horaInicio: value("itinerarioHoraInicio") || null, horaFin: value("itinerarioHoraFin") || null, actividad: value("itinerarioActividad"), descripcion: value("itinerarioDescripcion") || null };
}

function assignmentForm() {
    return `<div class="admin-form-grid">${select("asignacionSalida", "Salida", state.departures, "salidaId", "paquete", "")}
        ${select("asignacionServicio", "Servicio", [{ proveedorServicioId: "", nombre: "Sin servicio" }, ...state.services], "proveedorServicioId", "nombre", "")}
        <textarea id="asignacionObservaciones" class="swal2-textarea" placeholder="Observaciones"></textarea></div>`;
}

function readAssignmentForm() {
    const serviceId = value("asignacionServicio");
    return { salidaId: Number(value("asignacionSalida")), proveedorServicioId: serviceId ? Number(serviceId) : null, guiaId: null, transporteId: null, hotelId: null, habitacionId: null, observaciones: value("asignacionObservaciones") || null };
}

function matchesText(value) {
    const text = document.getElementById("buscarOperacion")?.value.trim().toLowerCase() || "";
    return String(value || "").toLowerCase().includes(text);
}

function matchesType(...values) {
    const selected = document.getElementById("filtroTipoOperacion")?.value || "";
    return !selected || values.some(value => String(value || "").toUpperCase() === selected);
}

function select(id, label, items, valueKey, textKey, selected) {
    return `<select id="${id}" class="swal2-input" aria-label="${escapeHtml(label)}">${items.map(item => `<option value="${escapeHtml(item[valueKey])}" ${String(item[valueKey]) === String(selected) ? "selected" : ""}>${escapeHtml(item[textKey])}</option>`).join("")}</select>`;
}

function tableCard(left, right, table) {
    return `<div class="admin-table-card"><div class="admin-table-meta"><span>${escapeHtml(left)}</span><span>${escapeHtml(right)}</span></div><div class="wp-table-wrap">${table}</div></div>`;
}

function value(id) {
    return document.getElementById(id)?.value?.trim() || "";
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
    const element = document.getElementById("operacionStatus");
    if (element) element.textContent = message;
}
