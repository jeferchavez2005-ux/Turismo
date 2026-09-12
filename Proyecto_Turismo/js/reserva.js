import { ApiError, api } from "./api.js";

const state = {
    destinoId: localStorage.getItem("destinoIdSeleccionado"),
    destino: localStorage.getItem("destinoSeleccionado") || "Selecciona un destino",
    imagen: localStorage.getItem("imgSeleccionada") || "",
    paquete: null,
    salida: null,
    tarifa: null,
    precio: Number.parseFloat(localStorage.getItem("precioSeleccionado") || "0") || 0,
    user: null,
    client: null
};

document.addEventListener("DOMContentLoaded", async () => {
    bindSummary();
    document.getElementById("resPasajeros")?.addEventListener("change", updateTotal);
    document.getElementById("formReserva")?.addEventListener("submit", submitReservation);
    document.getElementById("selectorDestinoReserva")?.addEventListener("change", selectDestination);
    try {
        await validateAuthenticatedClient();
        await ensureDestinationSelected();
        await loadReservationContext();
        await loadProfile();
        updateTotal();
    } catch (error) {
        handleInitialError(error);
    }
});

function bindSummary() {
    text("sumDestino", state.destino);
    const image = document.getElementById("sumImg");
    if (image) {
        image.src = state.imagen || "Img/machupichu.jpg";
        image.alt = state.destino;
    }
    text("sumPrecioUnitario", money(state.precio));
    updateTotal();
}

function handleInitialError(error) {
    const message = error instanceof ApiError ? error.message : "No se pudo cargar la informacion de reserva.";
    showMessage(message, true);
    if (error instanceof ApiError && error.status === 401) {
        const form = document.getElementById("formReserva");
        const login = document.createElement("a");
        login.className = "wp-btn wp-btn-primary";
        login.href = "login.html?returnUrl=%2Freserva.html";
        login.textContent = "Iniciar sesión";
        const register = document.createElement("a");
        register.className = "wp-btn wp-btn-outline";
        register.href = "registro.html?returnUrl=%2Freserva.html";
        register.textContent = "Registrarse";
        const actions = document.createElement("div");
        actions.className = "checkout-actions";
        actions.append(login, register);
        form?.prepend(actions);
    }
}

async function validateAuthenticatedClient() {
    try {
        state.user = await api("/api/auth/me");
    } catch (error) {
        localStorage.setItem("wp_auth_return_url", "/reserva.html");
        throw new ApiError("Debes iniciar sesión para realizar una reserva.", error.status || 401);
    }
    if (!(state.user.roles || []).includes("CLIENTE")) {
        throw new ApiError("Solo una cuenta Cliente puede realizar reservas desde este flujo.", 403);
    }
}

async function ensureDestinationSelected() {
    if (state.destinoId) return;
    const panel = document.getElementById("destinoSelectorPanel");
    const selector = document.getElementById("selectorDestinoReserva");
    const destinations = await api("/api/catalogo/destinos");
    if (panel) panel.hidden = false;
    if (selector) {
        selector.innerHTML = `<option value="">Selecciona un destino</option>${destinations.map(destino => `<option value="${destino.destinoId}" data-name="${escapeHtml(destino.nombre)}" data-price="${destino.precioDesde ?? 0}" data-img="${escapeHtml(destino.imagenUrl || "")}">${escapeHtml(destino.nombre)} - ${escapeHtml(destino.ciudad)}</option>`).join("")}`;
    }
    throw new ApiError("Selecciona un destino antes de continuar con la reserva.", 400);
}

function selectDestination(event) {
    const option = event.target.selectedOptions[0];
    if (!option?.value) return;
    state.destinoId = option.value;
    state.destino = option.dataset.name || option.textContent;
    state.precio = Number(option.dataset.price || 0);
    state.imagen = option.dataset.img || "Img/machupichu.jpg";
    localStorage.setItem("destinoIdSeleccionado", state.destinoId);
    localStorage.setItem("destinoSeleccionado", state.destino);
    localStorage.setItem("precioSeleccionado", String(state.precio));
    localStorage.setItem("imgSeleccionada", state.imagen);
    document.getElementById("destinoSelectorPanel").hidden = true;
    bindSummary();
    loadReservationContext().then(updateTotal).catch(error => showMessage(error.message, true));
}

async function loadReservationContext() {
    if (!state.destinoId) throw new ApiError("Seleccione un destino desde el catalogo antes de reservar.", 400);
    const packages = await api(`/api/catalogo/paquetes?destinoId=${encodeURIComponent(state.destinoId)}`);
    state.paquete = packages[0];
    if (!state.paquete) throw new ApiError("Este destino aun no tiene paquetes disponibles.", 404);
    const detail = await api(`/api/catalogo/paquetes/${encodeURIComponent(state.paquete.paqueteId)}`);
    state.tarifa = detail.tarifas?.[0];
    if (!state.tarifa) throw new ApiError("El paquete seleccionado no tiene tarifas activas.", 404);
    state.precio = Number(state.tarifa.precioBase);
    const availability = await api(`/api/operacion/disponibilidad?paqueteId=${encodeURIComponent(state.paquete.paqueteId)}`);
    state.salida = availability.find(x => !x.estaBloqueada && x.cupoDisponible > 0);
    if (!state.salida) throw new ApiError("No hay salidas disponibles para este paquete.", 409);
    const date = document.getElementById("resFecha");
    if (date) {
        date.value = state.salida.fechaInicio;
        date.min = state.salida.fechaInicio;
        date.max = state.salida.fechaInicio;
    }
    text("sumPrecioUnitario", money(state.precio));
}

async function loadProfile() {
    const client = await api("/api/clientes/me");
    state.client = client;
    document.getElementById("resEmail").value = state.user.email || "";
    if (client?.existe && client.perfil) {
        document.getElementById("resNombres").value = client.perfil.nombres || "";
        document.getElementById("resApellidos").value = client.perfil.apellidos || "";
        document.getElementById("resTel").value = client.perfil.telefono || "";
    } else {
        showMessage("Completa tus datos personales antes de continuar con la reserva.", true);
    }
}

async function submitReservation(event) {
    event.preventDefault();
    clearMessage();
    if (!state.salida || !state.tarifa) {
        showMessage("No hay una salida disponible para reservar.", true);
        return;
    }
    if (!isProfileComplete()) {
        showMessage("Completa nombres, apellidos y teléfono antes de reservar.", true);
        return;
    }
    const button = event.submitter || document.querySelector("#formReserva button[type='submit']");
    button.disabled = true;
    button.textContent = "Creando reserva...";
    try {
        await ensureClientProfile();
        const passengerIds = await ensurePassengers();
        const reserva = await api("/api/reservas", {
            method: "POST",
            body: {
                salidaId: Number(state.salida.salidaId),
                tarifaId: Number(state.tarifa.tarifaId),
                pasajeros: passengerIds.map((id, index) => ({
                    pasajeroId: Number(id),
                    tipoPasajero: "ADULTO",
                    esTitular: index === 0
                })),
                observaciones: value("resObservaciones")
            }
        });
        localStorage.setItem("wp_reserva_id", reserva.reservaId);
        localStorage.setItem("wp_codigo_reserva", reserva.codigoReserva);
        localStorage.setItem("wp_total_pagar", money(reserva.total));
        localStorage.setItem("wp_detalle_pasajeros", `${passengerIds.length}x Adultos (${money(state.precio)} c/u)`);
        localStorage.setItem("totalReserva", String(reserva.total));
        localStorage.setItem("fechaReserva", state.salida.fechaInicio);
        window.location.href = "pago.html";
    } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
            window.location.href = "login.html";
            return;
        }
        showMessage(error instanceof ApiError ? error.message : "No se pudo crear la reserva.", true);
    } finally {
        button.disabled = false;
        button.innerHTML = 'Continuar al pago <i class="fa-solid fa-arrow-right"></i>';
    }
}

async function ensureClientProfile() {
    await api("/api/clientes/me", {
        method: "PUT",
        body: {
            nombres: value("resNombres"),
            apellidos: value("resApellidos"),
            telefono: value("resTel"),
            fechaNacimiento: null,
            genero: null
        }
    });
}

async function ensurePassengers() {
    const count = Number.parseInt(document.getElementById("resPasajeros")?.value || "1", 10);
    const existing = await api("/api/reservas/me/pasajeros");
    if (existing.length < count) {
        throw new ApiError("Debes registrar un pasajero antes de continuar. Ve a Mi cuenta y agrega los pasajeros del viaje.", 400);
    }
    const selected = existing.slice(0, count).map(x => x.pasajeroId);
    return selected;
}

function isProfileComplete() {
    return Boolean(value("resNombres") && value("resApellidos") && value("resTel"));
}

function updateTotal() {
    const count = Number.parseInt(document.getElementById("resPasajeros")?.value || "1", 10);
    text("sumPasajerosNum", String(count));
    text("sumTotal", money(state.precio * count));
}

function showMessage(message, isError = false) {
    let box = document.getElementById("reservaMensaje");
    if (!box) {
        box = document.createElement("p");
        box.id = "reservaMensaje";
        box.className = "wp-alert";
        box.setAttribute("role", "status");
        box.setAttribute("aria-live", "polite");
        document.getElementById("formReserva")?.prepend(box);
    }
    box.textContent = message;
    box.className = `wp-alert ${isError ? "wp-alert-danger" : "wp-alert-success"}`;
}

function clearMessage() {
    const box = document.getElementById("reservaMensaje");
    if (box) box.textContent = "";
}

function value(id) {
    return document.getElementById(id)?.value.trim() || "";
}

function text(id, content) {
    const node = document.getElementById(id);
    if (node) node.textContent = content;
}

function money(value) {
    return `S/ ${Number(value || 0).toFixed(2)}`;
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, char => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    })[char]);
}
