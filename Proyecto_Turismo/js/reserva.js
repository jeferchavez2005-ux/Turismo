import { ApiError, api, appPath, appUrl } from "./api.js";
import { errorMessageWithTrace, t } from "./frontend-utils.js";
import { getDestinationImage } from "./destination-images.js";

const params = new URLSearchParams(location.search);
const context = readJson(sessionStorage, "wp_booking_context");
const draftKey = "wp_reservation_draft";
const state = { destinoId: params.get("destinoId") || context.destinoId || localStorage.getItem("destinoIdSeleccionado"), paqueteId: params.get("paqueteId") || context.paqueteId || null, destino: context.destino || localStorage.getItem("destinoSeleccionado") || "", imagen: context.imagen || localStorage.getItem("imgSeleccionada") || "", precio: Number(context.precio ?? localStorage.getItem("precioSeleccionado") ?? 0), user: null, profile: null, paquete: null, tarifa: null, salida: null, salidas: [], pasajeros: [], companions: [] };

document.addEventListener("DOMContentLoaded", init);

async function init() {
    bindSummary();
    document.getElementById("formReserva")?.addEventListener("submit", submitReservation);
    document.getElementById("selectorDestinoReserva")?.addEventListener("change", selectDestination);
    document.getElementById("resFecha")?.addEventListener("change", selectDeparture);
    document.getElementById("addCompanion")?.addEventListener("click", addCompanion);
    document.getElementById("principalCategory")?.addEventListener("change", updateTotal);
    document.getElementById("resObservaciones")?.addEventListener("input", saveDraft);
    try {
        await validateAuthenticatedClient();
        await Promise.all([loadProfile(), loadPassengers(), loadDestinations()]);
        restoreDraft();
        if (state.destinoId) await loadReservationContext();
        else showMessage(t("booking.destination.required", "Selecciona un destino antes de continuar."), true);
    } catch (error) { handleError(error); }
    updateTotal();
}

async function validateAuthenticatedClient() {
    try { state.user = await api("/api/auth/me"); }
    catch (error) {
        if (error instanceof ApiError && error.status === 401) {
            const returnUrl = appPath("reserva.html") + location.search;
            localStorage.setItem("wp_auth_return_url", returnUrl);
            location.replace(`${appUrl("login.html")}?returnUrl=${encodeURIComponent(returnUrl)}`);
        }
        throw error;
    }
    if (!(state.user.roles || []).includes("CLIENTE")) throw new ApiError(t("booking.auth.clientOnly", "Solo una cuenta de cliente puede reservar."), 403);
}

async function loadProfile() {
    const response = await api("/api/clientes/me");
    state.profile = response?.perfil || null;
    if (!state.profile) throw new ApiError(t("booking.travelers.profileRequired", "Completa tu perfil en Mi cuenta antes de reservar."), 409);
    text("principalName", `${state.profile.nombres} ${state.profile.apellidos}`);
    text("principalEmail", state.profile.email || state.user.email || "—");
    text("principalPhone", state.profile.telefono || "—");
}

async function loadPassengers() { state.pasajeros = await api("/api/reservas/me/pasajeros"); }

async function loadDestinations() {
    const destinations = await api("/api/catalogo/destinos");
    const select = document.getElementById("selectorDestinoReserva");
    select.innerHTML = `<option value="">${escapeHtml(t("booking.destination.select", "Selecciona un destino"))}</option>` + destinations.map(item => `<option value="${item.destinoId}" data-name="${escapeHtml(item.nombre)}" data-price="${item.precioDesde || 0}" data-img="${escapeHtml(item.imagenUrl || "")}">${escapeHtml(item.nombre)} · ${escapeHtml(item.ciudad)}</option>`).join("");
    select.value = state.destinoId || "";
    const selected = destinations.find(item => String(item.destinoId) === String(state.destinoId));
    if (selected) {
        state.destino = selected.nombre; state.imagen = getDestinationImage(selected, destinations);
        document.getElementById("destinoSelectorTitle").hidden = true;
        document.getElementById("destinoSelectorHelp").hidden = true;
    }
    document.getElementById("destinoSelectorPanel").hidden = false;
    persistContext(); bindSummary();
}

async function selectDestination(event) {
    const option = event.target.selectedOptions[0];
    if (!option?.value) return;
    state.destinoId = option.value; state.destino = option.dataset.name; state.precio = Number(option.dataset.price || 0);
    state.imagen = getDestinationImage({ destino: state.destino, imagenUrl: option.dataset.img }); state.paqueteId = null;
    document.getElementById("destinoSelectorTitle").hidden = true; document.getElementById("destinoSelectorHelp").hidden = true;
    persistContext(); bindSummary();
    try { await loadReservationContext(); } catch (error) { handleError(error); }
}

async function loadReservationContext() {
    state.salida = null; updateContinueState();
    const packages = await api(`/api/catalogo/paquetes?destinoId=${encodeURIComponent(state.destinoId)}`);
    state.paquete = packages.find(item => String(item.paqueteId) === String(state.paqueteId)) || packages[0];
    if (!state.paquete) throw new ApiError(t("booking.trip.empty", "No hay salidas disponibles para la selección actual."), 404);
    const detail = await api(`/api/catalogo/paquetes/${encodeURIComponent(state.paquete.paqueteId)}`);
    state.tarifa = detail.tarifas?.[0];
    if (!state.tarifa) throw new ApiError(t("booking.trip.noFare", "La experiencia no tiene una tarifa activa."), 404);
    state.paqueteId = String(state.paquete.paqueteId); state.precio = Number(state.tarifa.precioBase);
    state.salidas = (await api(`/api/operacion/disponibilidad?paqueteId=${encodeURIComponent(state.paqueteId)}`)).filter(item => !item.estaBloqueada && Number(item.cupoDisponible) > 0);
    renderDepartures(); text("sumPaquete", state.paquete.nombre); text("sumPrecioUnitario", money(state.precio)); persistContext(); updateTotal();
}

function renderDepartures() {
    const select = document.getElementById("resFecha"); select.disabled = !state.salidas.length;
    select.innerHTML = state.salidas.length ? `<option value="">${escapeHtml(t("booking.trip.selectDeparture", "Selecciona una salida"))}</option>${state.salidas.map(item => `<option value="${item.salidaId}">${escapeHtml(formatDeparture(item))}</option>`).join("")}` : `<option value="">${escapeHtml(t("booking.trip.empty", "No hay salidas disponibles para la selección actual."))}</option>`;
}

function selectDeparture(event) { state.salida = state.salidas.find(item => String(item.salidaId) === event.target.value) || null; updateContinueState(); }
function formatDeparture(item) { const date = new Intl.DateTimeFormat(document.documentElement.lang || "es", { dateStyle: "long" }).format(new Date(`${item.fechaInicio}T00:00:00`)); return `${date} · ${item.cupoDisponible} ${t("booking.trip.seats", "cupos disponibles")}`; }

function addCompanion() {
    if (state.companions.length >= 19) return showMessage(t("booking.passengers.maximum", "Puedes registrar hasta 19 acompañantes."), true);
    state.companions.push({ id: crypto.randomUUID?.() || `${Date.now()}-${state.companions.length}` }); renderCompanions();
}

function renderCompanions() {
    const list = document.getElementById("companionList"); list.innerHTML = state.companions.map(companionHtml).join("");
    list.querySelectorAll(".remove-companion").forEach(button => button.addEventListener("click", () => { state.companions = state.companions.filter(item => item.id !== button.dataset.id); renderCompanions(); saveDraft(); }));
    list.querySelectorAll("input, select, textarea").forEach(control => control.addEventListener("input", () => { updateTotal(); saveDraft(); })); updateTotal();
}

function companionHtml(item, index) {
    return `<section class="companion-card" data-id="${escapeHtml(item.id)}"><div class="traveler-heading"><h4>${escapeHtml(t("booking.travelers.companion", "Acompañante"))} ${index + 1}</h4><button type="button" class="remove-companion" data-id="${escapeHtml(item.id)}">${escapeHtml(t("booking.travelers.remove", "Eliminar acompañante"))}</button></div><div class="form-row"><label>${escapeHtml(t("account.passengers.names", "Nombres"))}<input class="wp-input companion-names" value="${escapeHtml(item.nombres || "")}" maxlength="120" required></label><label>${escapeHtml(t("account.passengers.lastNames", "Apellidos"))}<input class="wp-input companion-lastnames" value="${escapeHtml(item.apellidos || "")}" maxlength="120" required></label></div><div class="form-row"><label>${escapeHtml(t("account.passengers.birthDate", "Fecha de nacimiento"))}<input type="date" class="wp-input companion-birth" value="${escapeHtml(item.fechaNacimiento || "")}" max="${previousDateIso()}" required></label><label>${escapeHtml(t("booking.passengers.category", "Categoría"))}<select class="wp-select companion-category">${categoryOptions(item.tipoPasajero)}</select></label></div><label>${escapeHtml(t("booking.notes.label", "Requerimientos especiales (opcional)"))}<textarea class="wp-textarea companion-requirements" maxlength="500">${escapeHtml(item.requerimientosEspeciales || "")}</textarea></label></section>`;
}

function categoryOptions(selected = "ADULTO") { return [["ADULTO","adult"],["NINO","child"],["INFANTE","infant"],["ADULTO_MAYOR","senior"]].map(([value,key]) => `<option value="${value}" ${value === selected ? "selected" : ""}>${escapeHtml(t(`booking.passengers.${key}`, value))}</option>`).join(""); }
function readCompanionForms() { return [...document.querySelectorAll(".companion-card")].map(card => ({ id: card.dataset.id, nombres: card.querySelector(".companion-names").value.trim(), apellidos: card.querySelector(".companion-lastnames").value.trim(), fechaNacimiento: card.querySelector(".companion-birth").value, tipoPasajero: card.querySelector(".companion-category").value, requerimientosEspeciales: card.querySelector(".companion-requirements").value.trim() })); }

async function submitReservation(event) {
    event.preventDefault(); clearMessage();
    if (!state.salida || !state.tarifa) return showMessage(t("booking.trip.selectDeparture", "Selecciona una salida disponible."), true);
    if (!state.profile?.telefono) return showMessage(t("booking.travelers.profileRequired", "Completa tu teléfono en Mi cuenta antes de reservar."), true);
    const button = event.submitter; button.disabled = true;
    try {
        const passengers = await resolvePassengers();
        const reserva = await api("/api/reservas", { method: "POST", body: { salidaId: Number(state.salida.salidaId), tarifaId: Number(state.tarifa.tarifaId), pasajeros: passengers, observaciones: value("resObservaciones") || null } });
        localStorage.setItem("wp_reserva_id", reserva.reservaId); localStorage.setItem("wp_codigo_reserva", reserva.codigoReserva); localStorage.setItem("wp_total_pagar", money(reserva.total)); localStorage.setItem("totalReserva", String(reserva.total)); localStorage.setItem("fechaReserva", state.salida.fechaInicio);
        sessionStorage.removeItem(draftKey); location.href = appUrl("pago.html");
    } catch (error) { showMessage(errorMessageWithTrace(error, t("booking.create.error", "No se pudo crear la reserva.")), true); }
    finally { button.disabled = false; button.innerHTML = `<span data-i18n="booking.continue">${escapeHtml(t("booking.continue", "Registrar reserva"))}</span> <i class="fa-solid fa-arrow-right"></i>`; }
}

async function resolvePassengers() {
    const companions = readCompanionForms();
    for (const companion of companions) if (!companion.nombres || !companion.apellidos || !isValidPastDate(companion.fechaNacimiento)) throw new ApiError(t("booking.travelers.invalidCompanion", "Completa los datos y una fecha válida para cada acompañante."), 400);
    const principal = await findOrCreatePassenger({ nombres: state.profile.nombres, apellidos: state.profile.apellidos, fechaNacimiento: state.profile.fechaNacimiento, requerimientosEspeciales: null });
    const result = [{ pasajeroId: Number(principal.pasajeroId), tipoPasajero: document.getElementById("principalCategory").value, esTitular: true }];
    for (const companion of companions) { const saved = await findOrCreatePassenger(companion); result.push({ pasajeroId: Number(saved.pasajeroId), tipoPasajero: companion.tipoPasajero, esTitular: false }); }
    return result;
}

async function findOrCreatePassenger(person) {
    const match = state.pasajeros.find(item => normalize(item.nombres) === normalize(person.nombres) && normalize(item.apellidos) === normalize(person.apellidos) && String(item.fechaNacimiento || "") === String(person.fechaNacimiento || ""));
    if (match) return match;
    const created = await api("/api/reservas/me/pasajeros", { method: "POST", body: { nombres: person.nombres, apellidos: person.apellidos, fechaNacimiento: person.fechaNacimiento || null, requerimientosEspeciales: person.requerimientosEspeciales || null } }); state.pasajeros.push(created); return created;
}

function updateTotal() {
    const companions = readCompanionForms(); const count = 1 + companions.length; const childPrice = Number(state.tarifa?.precioNino ?? state.precio); const types = [document.getElementById("principalCategory")?.value || "ADULTO", ...companions.map(item => item.tipoPasajero)];
    const total = types.reduce((sum, type) => sum + (["NINO","INFANTE"].includes(type) ? childPrice : state.precio), 0);
    text("resPasajerosCount", String(count)); text("sumPasajerosNum", String(count)); text("sumTotal", money(total)); text("travelerCountLabel", `${count} ${t(count === 1 ? "booking.travelers.one" : "booking.travelers.many", count === 1 ? "viajero" : "viajeros")}`); updateContinueState();
}

function updateContinueState() { document.getElementById("continueToPayment").disabled = !(state.salida && state.tarifa && state.profile?.telefono); }
function bindSummary() { text("sumDestino", state.destino || "—"); const image = document.getElementById("sumImg"); image.src = getDestinationImage({ destino: state.destino, imagenUrl: state.imagen }); image.alt = state.destino || "WayPath"; text("sumPrecioUnitario", money(state.precio)); }
function persistContext() { sessionStorage.setItem("wp_booking_context", JSON.stringify({ destinoId: state.destinoId, destino: state.destino, paqueteId: state.paqueteId, precio: state.precio, imagen: state.imagen })); }
function saveDraft() { sessionStorage.setItem(draftKey, JSON.stringify({ destinoId: state.destinoId, observaciones: value("resObservaciones"), companions: readCompanionForms() })); }
function restoreDraft() { const draft = readJson(sessionStorage, draftKey); if (draft.destinoId && String(draft.destinoId) !== String(state.destinoId)) return; state.companions = draft.companions || []; if (draft.observaciones) document.getElementById("resObservaciones").value = draft.observaciones; renderCompanions(); }
function handleError(error) { showMessage(errorMessageWithTrace(error, t("booking.context.loadError", "No se pudo cargar la reserva.")), true); }
function showMessage(message, error = false) { let box = document.getElementById("reservaMensaje"); if (!box) { box = document.createElement("div"); box.id = "reservaMensaje"; document.getElementById("formReserva")?.prepend(box); } box.className = `wp-alert ${error ? "wp-alert-danger" : "wp-alert-success"}`; box.textContent = message; }
function clearMessage() { document.getElementById("reservaMensaje")?.remove(); }
function readJson(storage, key) { try { return JSON.parse(storage.getItem(key) || "{}") || {}; } catch { return {}; } }
function isValidPastDate(value) { if (!/^\d{4}-\d{2}-\d{2}$/.test(value || "")) return false; const date = new Date(`${value}T00:00:00`); const today = new Date(); today.setHours(0,0,0,0); return !Number.isNaN(date.getTime()) && date < today && date.toISOString().slice(0,10) === value; }
function previousDateIso() { const date = new Date(); date.setDate(date.getDate() - 1); return date.toISOString().slice(0,10); }
function normalize(value) { return String(value || "").trim().toLocaleLowerCase("es"); }
function value(id) { return document.getElementById(id)?.value?.trim() || ""; }
function text(id, content) { const node = document.getElementById(id); if (node) node.textContent = content; }
function money(value) { return Number(value || 0).toLocaleString(document.documentElement.lang || "es-PE", { style: "currency", currency: "PEN" }); }
function escapeHtml(value) { return String(value ?? "").replace(/[&<>"']/g, char => ({"&":"&amp;","<":"&lt;",">":"&gt;","\"":"&quot;","'":"&#39;"})[char]); }
window.addEventListener("waypath:languagechange", () => { renderCompanions(); if (state.salidas.length) renderDepartures(); updateTotal(); });
