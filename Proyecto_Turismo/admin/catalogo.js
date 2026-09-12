import { api } from "../js/api.js";
import { adminEmpty, adminError, adminLoading, can, escapeHtml, initAdminPage, stateBadge } from "./admin-shared.js";

const state = {
    activeTab: "destinos",
    destinations: [],
    packages: [],
    packageDetails: new Map(),
    options: { categorias: [], ciudades: [] },
    user: null
};

document.addEventListener("DOMContentLoaded", init);

async function init() {
    try {
        state.user = await initAdminPage({ active: "Catálogo", title: "Catálogo" });
    } catch {
        return;
    }
    if (!can(state.user, "CATALOGO_DESTINOS_GESTIONAR")) return;
    bindEvents();
    await loadData();
}

function bindEvents() {
    document.querySelectorAll(".admin-tab").forEach(button => {
        button.addEventListener("click", () => {
            state.activeTab = button.dataset.tab || "destinos";
            document.querySelectorAll(".admin-tab").forEach(tab => tab.classList.toggle("active", tab === button));
            document.getElementById("crearCatalogo").hidden = state.activeTab === "tarifas";
            render();
        });
    });
    document.getElementById("buscarCatalogo")?.addEventListener("input", render);
    document.getElementById("filtroDestacadoCatalogo")?.addEventListener("change", render);
    document.getElementById("recargarCatalogo")?.addEventListener("click", loadData);
    document.getElementById("crearCatalogo")?.addEventListener("click", () => {
        if (state.activeTab === "destinos") createOrEditDestination();
        if (state.activeTab === "paquetes") createOrEditPackage();
    });
}

async function loadData() {
    const host = document.getElementById("catalogoTableHost");
    host.innerHTML = adminLoading("Cargando catálogo...");
    try {
        const [destinations, packages, options] = await Promise.all([
            api("/api/catalogo/destinos"),
            api("/api/catalogo/paquetes"),
            api("/api/catalogo/opciones")
        ]);
        state.destinations = destinations || [];
        state.packages = packages || [];
        state.options = options || { categorias: [], ciudades: [] };
        await loadPackageDetails();
        render();
        status(`Catálogo cargado: ${state.destinations.length} destinos y ${state.packages.length} paquetes.`);
    } catch (error) {
        host.innerHTML = adminError(error.message || "No se pudo cargar el catálogo.");
        status("No se pudo cargar el catálogo.");
    }
}

async function loadPackageDetails() {
    state.packageDetails.clear();
    await Promise.all(state.packages.map(async item => {
        try {
            const detail = await api(`/api/catalogo/paquetes/${encodeURIComponent(item.paqueteId)}`);
            state.packageDetails.set(String(item.paqueteId), detail);
        } catch {
            state.packageDetails.set(String(item.paqueteId), null);
        }
    }));
}

function render() {
    const host = document.getElementById("catalogoTableHost");
    const views = { destinos: renderDestinations, paquetes: renderPackages, tarifas: renderRates };
    host.innerHTML = views[state.activeTab]?.() || renderDestinations();
    bindTableActions(host);
}

function renderDestinations() {
    const rows = filteredDestinations();
    if (!rows.length) return adminEmpty("Sin destinos", "No hay destinos que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} destinos visibles`, "API: /api/catalogo/destinos", `
        <table class="wp-table">
            <thead><tr><th>Destino</th><th>Ubicación</th><th>Categorías</th><th>Precio desde</th><th>Destacado</th><th style="text-align:right;">Acciones</th></tr></thead>
            <tbody>${rows.map(destinationRow).join("")}</tbody>
        </table>`);
}

function destinationRow(item) {
    return `
        <tr>
            <td><div class="admin-row-title"><strong>${escapeHtml(item.nombre)}</strong><span>${escapeHtml(item.slug)}</span></div></td>
            <td><div class="admin-row-title"><strong>${escapeHtml(item.ciudad)}</strong><span>${escapeHtml(item.region)} · ${escapeHtml(item.pais)}</span></div></td>
            <td>${(item.categorias || []).map(category => escapeHtml(category.nombre)).join(", ") || "-"}</td>
            <td>${item.precioDesde ? money(item.precioDesde, item.moneda) : "-"}</td>
            <td>${item.esDestacado ? stateBadge("ACTIVO") : stateBadge("INACTIVO")}</td>
            <td><div class="admin-actions-cell">
                <button class="admin-action-btn" data-action="destination-detail" data-id="${escapeHtml(item.destinoId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                <button class="admin-action-btn" data-action="destination-edit" data-id="${escapeHtml(item.destinoId)}"><i class="fa-solid fa-pen"></i> Editar</button>
                <button class="admin-action-btn" data-action="destination-state" data-id="${escapeHtml(item.destinoId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>
            </div></td>
        </tr>`;
}

function renderPackages() {
    const rows = filteredPackages();
    if (!rows.length) return adminEmpty("Sin paquetes", "No hay paquetes que coincidan con los filtros actuales.");
    return tableCard(`${rows.length} paquetes visibles`, "API: /api/catalogo/paquetes", `
        <table class="wp-table">
            <thead><tr><th>Paquete</th><th>Destino</th><th>Duración</th><th>Dificultad</th><th>Precio desde</th><th>Destacado</th><th style="text-align:right;">Acciones</th></tr></thead>
            <tbody>${rows.map(packageRow).join("")}</tbody>
        </table>`);
}

function packageRow(item) {
    const destination = state.destinations.find(destino => String(destino.destinoId) === String(item.destinoId));
    return `
        <tr>
            <td><div class="admin-row-title"><strong>${escapeHtml(item.nombre)}</strong><span>${escapeHtml(item.slug)}</span></div></td>
            <td>${escapeHtml(destination?.nombre || item.destinoId)}</td>
            <td>${escapeHtml(item.duracionDias)} días / ${escapeHtml(item.duracionNoches)} noches</td>
            <td>${escapeHtml(item.nivelDificultad || "-")}</td>
            <td>${item.precioDesde ? money(item.precioDesde, item.moneda) : "-"}</td>
            <td>${item.esDestacado ? stateBadge("ACTIVO") : stateBadge("INACTIVO")}</td>
            <td><div class="admin-actions-cell">
                <button class="admin-action-btn" data-action="package-detail" data-id="${escapeHtml(item.paqueteId)}"><i class="fa-solid fa-eye"></i> Detalle</button>
                <button class="admin-action-btn" data-action="package-edit" data-id="${escapeHtml(item.paqueteId)}"><i class="fa-solid fa-pen"></i> Editar</button>
                <button class="admin-action-btn" data-action="package-state" data-id="${escapeHtml(item.paqueteId)}"><i class="fa-solid fa-arrows-rotate"></i> Estado</button>
            </div></td>
        </tr>`;
}

function renderRates() {
    const rows = filteredRates();
    if (!rows.length) return adminEmpty("Sin tarifas", "No hay tarifas publicadas en los paquetes cargados.");
    return tableCard(`${rows.length} tarifas visibles`, "Solo lectura desde detalle de paquetes", `
        <table class="wp-table">
            <thead><tr><th>Paquete</th><th>Tipo tarifa</th><th>Precio adulto</th><th>Precio niño</th><th>Moneda</th><th>Vigencia</th></tr></thead>
            <tbody>${rows.map(rateRow).join("")}</tbody>
        </table>`);
}

function rateRow(item) {
    return `
        <tr>
            <td>${escapeHtml(item.paquete)}</td>
            <td>${escapeHtml(item.tipoTarifa)}</td>
            <td>${money(item.precioBase, item.moneda)}</td>
            <td>${item.precioNino ? money(item.precioNino, item.moneda) : "-"}</td>
            <td>${escapeHtml(item.moneda)}</td>
            <td>${formatDate(item.vigenciaInicio)} - ${formatDate(item.vigenciaFin)}</td>
        </tr>`;
}

function filteredDestinations() {
    const featured = document.getElementById("filtroDestacadoCatalogo")?.value;
    return state.destinations.filter(item => matchesText(`${item.nombre} ${item.slug} ${item.ciudad} ${item.region} ${item.pais}`) && (featured === "" || String(item.esDestacado) === featured));
}

function filteredPackages() {
    const featured = document.getElementById("filtroDestacadoCatalogo")?.value;
    return state.packages.filter(item => matchesText(`${item.nombre} ${item.slug} ${item.nivelDificultad || ""}`) && (featured === "" || String(item.esDestacado) === featured));
}

function filteredRates() {
    return [...state.packageDetails.values()].filter(Boolean).flatMap(detail => (detail.tarifas || []).map(rate => ({ ...rate, paquete: detail.nombre })))
        .filter(item => matchesText(`${item.paquete} ${item.tipoTarifa} ${item.moneda}`));
}

function matchesText(value) {
    const text = document.getElementById("buscarCatalogo")?.value.trim().toLowerCase() || "";
    return String(value || "").toLowerCase().includes(text);
}

function bindTableActions(host) {
    host.querySelectorAll("[data-action='destination-detail']").forEach(button => button.addEventListener("click", () => showDestination(button.dataset.id)));
    host.querySelectorAll("[data-action='destination-edit']").forEach(button => button.addEventListener("click", () => createOrEditDestination(button.dataset.id)));
    host.querySelectorAll("[data-action='destination-state']").forEach(button => button.addEventListener("click", () => changeState("destinos", button.dataset.id)));
    host.querySelectorAll("[data-action='package-detail']").forEach(button => button.addEventListener("click", () => showPackage(button.dataset.id)));
    host.querySelectorAll("[data-action='package-edit']").forEach(button => button.addEventListener("click", () => createOrEditPackage(button.dataset.id)));
    host.querySelectorAll("[data-action='package-state']").forEach(button => button.addEventListener("click", () => changeState("paquetes", button.dataset.id)));
}

async function showDestination(id) {
    try {
        const detail = await api(`/api/catalogo/destinos/${encodeURIComponent(id)}`);
        modal("Detalle de destino", `
            <div class="admin-detail-list">
                <p><strong>Destino:</strong> ${escapeHtml(detail.nombre)}</p>
                <p><strong>Slug:</strong> ${escapeHtml(detail.slug)}</p>
                <p><strong>Ubicación:</strong> ${escapeHtml(detail.ciudad)} · ${escapeHtml(detail.region)} · ${escapeHtml(detail.pais)}</p>
                <p><strong>Categorías:</strong> ${(detail.categorias || []).map(item => escapeHtml(item.nombre)).join(", ") || "-"}</p>
                <p><strong>Atracciones:</strong> ${(detail.atracciones || []).map(item => escapeHtml(item.nombre)).join("<br>") || "Sin atracciones"}</p>
                <p><strong>Descripción:</strong> ${escapeHtml(detail.descripcionLarga || detail.descripcionCorta)}</p>
            </div>`);
    } catch (error) {
        toast(error.message || "No se pudo cargar el destino.", "error");
    }
}

function showPackage(id) {
    const detail = state.packageDetails.get(String(id));
    if (!detail) return toast("No se pudo cargar el paquete.", "error");
    modal("Detalle de paquete", `
        <div class="admin-detail-list">
            <p><strong>Paquete:</strong> ${escapeHtml(detail.nombre)}</p>
            <p><strong>Destino:</strong> ${escapeHtml(detail.destino)}</p>
            <p><strong>Duración:</strong> ${escapeHtml(detail.duracionDias)} días / ${escapeHtml(detail.duracionNoches)} noches</p>
            <p><strong>Incluye:</strong> ${(detail.incluye || []).map(escapeHtml).join("<br>") || "-"}</p>
            <p><strong>No incluye:</strong> ${(detail.noIncluye || []).map(escapeHtml).join("<br>") || "-"}</p>
            <p><strong>Tarifas:</strong> ${(detail.tarifas || []).map(rate => `${escapeHtml(rate.tipoTarifa)} · ${money(rate.precioBase, rate.moneda)}`).join("<br>") || "Sin tarifas"}</p>
        </div>`);
}

async function createOrEditDestination(id) {
    const current = id ? await api(`/api/catalogo/destinos/${encodeURIComponent(id)}`) : null;
    const result = await Swal.fire({
        title: id ? "Editar destino" : "Nuevo destino",
        html: destinationForm(current),
        width: 760,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: readDestinationForm
    });
    if (!result.isConfirmed) return;
    try {
        const url = id ? `/api/catalogo/destinos/${encodeURIComponent(id)}` : "/api/catalogo/destinos";
        await api(url, { method: id ? "PUT" : "POST", body: result.value });
        toast("Destino guardado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo guardar el destino.", "error");
    }
}

async function createOrEditPackage(id) {
    const current = id ? state.packageDetails.get(String(id)) : null;
    const result = await Swal.fire({
        title: id ? "Editar paquete" : "Nuevo paquete",
        html: packageForm(current),
        width: 720,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: readPackageForm
    });
    if (!result.isConfirmed) return;
    try {
        const url = id ? `/api/catalogo/paquetes/${encodeURIComponent(id)}` : "/api/catalogo/paquetes";
        await api(url, { method: id ? "PUT" : "POST", body: result.value });
        toast("Paquete guardado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo guardar el paquete.", "error");
    }
}

async function changeState(type, id) {
    const result = await Swal.fire({
        title: "Cambiar estado",
        html: `<select id="catalogoEstado" class="swal2-input"><option value="ACTIVO">ACTIVO</option><option value="INACTIVO">INACTIVO</option></select>`,
        showCancelButton: true,
        confirmButtonText: "Guardar",
        cancelButtonText: "Cancelar",
        confirmButtonColor: "#f97316",
        preConfirm: () => document.getElementById("catalogoEstado").value
    });
    if (!result.isConfirmed) return;
    try {
        await api(`/api/catalogo/${type}/${encodeURIComponent(id)}/state`, { method: "PUT", body: { codigo: result.value } });
        toast("Estado actualizado.");
        await loadData();
    } catch (error) {
        toast(error.message || "No se pudo cambiar el estado.", "error");
    }
}

function destinationForm(item) {
    return `
        <div class="admin-form-grid">
            ${select("destinoCiudad", "Ciudad", state.options.ciudades || [], "ciudadId", "nombre", cityId(item))}
            <input id="destinoNombre" class="swal2-input" placeholder="Nombre" value="${escapeHtml(item?.nombre || "")}">
            <input id="destinoSlug" class="swal2-input" placeholder="Slug" value="${escapeHtml(item?.slug || "")}">
            <input id="destinoDescripcionCorta" class="swal2-input" placeholder="Descripción corta" value="${escapeHtml(item?.descripcionCorta || "")}">
            <textarea id="destinoDescripcionLarga" class="swal2-textarea" placeholder="Descripción larga">${escapeHtml(item?.descripcionLarga || "")}</textarea>
            <input id="destinoAltitud" class="swal2-input" type="number" placeholder="Altitud metros" value="${escapeHtml(item?.altitudMetros || "")}">
            <label class="admin-check"><input id="destinoDestacado" type="checkbox" ${item?.esDestacado ? "checked" : ""}> Destacado</label>
            ${categoryChecks(item)}
        </div>`;
}

function packageForm(item) {
    return `
        <div class="admin-form-grid">
            ${select("paqueteDestino", "Destino", state.destinations, "destinoId", "nombre", item?.destinoId)}
            <input id="paqueteNombre" class="swal2-input" placeholder="Nombre" value="${escapeHtml(item?.nombre || "")}">
            <input id="paqueteSlug" class="swal2-input" placeholder="Slug" value="${escapeHtml(item?.slug || "")}">
            <textarea id="paqueteDescripcion" class="swal2-textarea" placeholder="Descripción">${escapeHtml(item?.descripcion || "")}</textarea>
            <input id="paqueteDias" class="swal2-input" type="number" min="1" placeholder="Duración días" value="${escapeHtml(item?.duracionDias || 1)}">
            <input id="paqueteNoches" class="swal2-input" type="number" min="0" placeholder="Duración noches" value="${escapeHtml(item?.duracionNoches || 0)}">
            <select id="paqueteDificultad" class="swal2-input">
                ${["BAJO", "MEDIO", "ALTO", "EXTREMO"].map(value => `<option value="${value}" ${item?.nivelDificultad === value ? "selected" : ""}>${value}</option>`).join("")}
            </select>
            <label class="admin-check"><input id="paqueteDestacado" type="checkbox" ${item?.esDestacado ? "checked" : ""}> Destacado</label>
        </div>`;
}

function readDestinationForm() {
    return {
        ciudadId: Number(document.getElementById("destinoCiudad").value),
        ubicacionTuristicaId: null,
        nombre: document.getElementById("destinoNombre").value.trim(),
        slug: document.getElementById("destinoSlug").value.trim(),
        descripcionCorta: document.getElementById("destinoDescripcionCorta").value.trim(),
        descripcionLarga: document.getElementById("destinoDescripcionLarga").value.trim() || null,
        altitudMetros: Number(document.getElementById("destinoAltitud").value) || null,
        esDestacado: document.getElementById("destinoDestacado").checked,
        categoriaIds: [...document.querySelectorAll("[data-categoria]:checked")].map(item => Number(item.value))
    };
}

function readPackageForm() {
    return {
        destinoId: Number(document.getElementById("paqueteDestino").value),
        nombre: document.getElementById("paqueteNombre").value.trim(),
        slug: document.getElementById("paqueteSlug").value.trim(),
        descripcion: document.getElementById("paqueteDescripcion").value.trim() || null,
        duracionDias: Number(document.getElementById("paqueteDias").value),
        duracionNoches: Number(document.getElementById("paqueteNoches").value),
        nivelDificultad: document.getElementById("paqueteDificultad").value,
        esDestacado: document.getElementById("paqueteDestacado").checked
    };
}

function select(id, label, items, valueKey, textKey, selected) {
    return `<select id="${id}" class="swal2-input" aria-label="${escapeHtml(label)}">${items.map(item => `<option value="${escapeHtml(item[valueKey])}" ${String(item[valueKey]) === String(selected) ? "selected" : ""}>${escapeHtml(item[textKey])}</option>`).join("")}</select>`;
}

function categoryChecks(item) {
    const selected = new Set((item?.categorias || []).map(category => Number(category.categoriaDestinoId)));
    return `<div class="admin-check-list">${(state.options.categorias || []).map(category => `<label class="admin-check"><input data-categoria type="checkbox" value="${category.categoriaDestinoId}" ${selected.has(category.categoriaDestinoId) ? "checked" : ""}> ${escapeHtml(category.nombre)}</label>`).join("")}</div>`;
}

function cityId(item) {
    const city = state.options.ciudades?.find(value => value.nombre === item?.ciudad);
    return city?.ciudadId;
}

function tableCard(left, right, table) {
    return `<div class="admin-table-card"><div class="admin-table-meta"><span>${escapeHtml(left)}</span><span>${escapeHtml(right)}</span></div><div class="wp-table-wrap">${table}</div></div>`;
}

function modal(title, html) {
    if (window.Swal) Swal.fire({ title, html, width: 780, confirmButtonColor: "#f97316" });
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
    const element = document.getElementById("catalogoStatus");
    if (element) element.textContent = message;
}
