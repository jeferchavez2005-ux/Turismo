import { ApiError, api, appPath, appUrl } from "./api.js";

const state = {
    destinoId: localStorage.getItem("destinoIdSeleccionado"),
    destino: localStorage.getItem("destinoSeleccionado") || "Selecciona un destino",
    imagen: localStorage.getItem("imgSeleccionada") || "",
    paquete: null,
    salida: null,
    tarifa: null,
    precio: Number.parseFloat(localStorage.getItem("precioSeleccionado") || "0") || 0,
    user: null,
    client: null,
    pasajeros: []
};

const t = (key, fallback) => {
    const translated = window.WayPathI18n?.t(key);
    return translated && translated !== key ? translated : fallback;
};

document.addEventListener("DOMContentLoaded", async () => {
    const addPassengerLink = document.querySelector(".passenger-add-link");
    if (addPassengerLink) {
        addPassengerLink.href = `${appUrl("cuenta.html")}?returnUrl=${encodeURIComponent(appPath("reserva.html"))}#passengersSection`;
    }
    bindSummary();
    document.getElementById("formReserva")?.addEventListener("submit", submitReservation);
    document.getElementById("selectorDestinoReserva")?.addEventListener("change", selectDestination);
    try {
        await validateAuthenticatedClient();
        const hasDestination = await ensureDestinationSelected();
        if (hasDestination) await loadReservationContext();
        await loadProfile();
        await loadPassengers();
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
        login.href = `${appUrl("login.html")}?returnUrl=${encodeURIComponent(appPath("reserva.html"))}`;
        login.textContent = "Iniciar sesión";
        const register = document.createElement("a");
        register.className = "wp-btn wp-btn-outline";
        register.href = `${appUrl("registro.html")}?returnUrl=${encodeURIComponent(appPath("reserva.html"))}`;
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
        localStorage.setItem("wp_auth_return_url", appPath("reserva.html"));
        throw new ApiError("Debes iniciar sesión para realizar una reserva.", error.status || 401);
    }
    if (!(state.user.roles || []).includes("CLIENTE")) {
        throw new ApiError("Solo una cuenta Cliente puede realizar reservas desde este flujo.", 403);
    }
}

async function ensureDestinationSelected() {
    if (state.destinoId) return true;
    const panel = document.getElementById("destinoSelectorPanel");
    const selector = document.getElementById("selectorDestinoReserva");
    const destinations = await api("/api/catalogo/destinos");
    if (panel) panel.hidden = false;
    if (selector) {
        selector.innerHTML = `<option value="">${escapeHtml(t("booking.destination.select", "Selecciona un destino"))}</option>${destinations.map(destino => `<option value="${destino.destinoId}" data-name="${escapeHtml(destino.nombre)}" data-price="${destino.precioDesde ?? 0}" data-img="${escapeHtml(destino.imagenUrl || "")}">${escapeHtml(destino.nombre)} - ${escapeHtml(destino.ciudad)}</option>`).join("")}`;
    }
    showMessage(t("booking.destination.required", "Selecciona un destino antes de continuar con la reserva."), true);
    return false;
}

async function selectDestination(event) {
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
    clearMessage();
    event.target.disabled = true;
    try {
        await loadReservationContext();
        updateTotal();
    } catch (error) {
        showMessage(error.message || t("booking.context.loadError", "No se pudo cargar la disponibilidad del destino."), true);
        document.getElementById("destinoSelectorPanel").hidden = false;
    } finally {
        event.target.disabled = false;
    }
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

async function loadPassengers() {
    const status = document.getElementById("passengerPickerStatus");
    status.textContent = t("booking.passengers.loading", "Cargando pasajeros...");
    status.classList.remove("is-error");
    try {
        state.pasajeros = await api("/api/reservas/me/pasajeros");
        renderPassengerPicker();
        status.textContent = state.pasajeros.length ? "" : t("booking.passengers.empty", "No tienes pasajeros registrados. Agrégalos en Mi cuenta antes de continuar.");
    } catch (error) {
        status.textContent = error.message || t("booking.passengers.loadError", "No pudimos cargar tus pasajeros.");
        status.classList.add("is-error");
    }
}

function renderPassengerPicker(selection = new Map()) {
    const list = document.getElementById("passengerPickerList");
    list.replaceChildren();
    state.pasajeros.forEach(passenger => {
        const row = document.createElement("div");
        row.className = "passenger-option";
        row.dataset.passengerId = passenger.pasajeroId;

        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.className = "passenger-select";
        checkbox.id = `passenger-${passenger.pasajeroId}`;
        checkbox.setAttribute("aria-label", `${t("booking.passengers.select", "Seleccionar a")} ${passenger.nombres} ${passenger.apellidos}`);

        const main = document.createElement("label");
        main.className = "passenger-option-main";
        main.htmlFor = checkbox.id;
        const name = document.createElement("strong");
        name.className = "passenger-option-name";
        name.textContent = `${passenger.nombres} ${passenger.apellidos}`;
        const meta = document.createElement("span");
        meta.className = "passenger-option-meta";
        meta.textContent = passenger.fechaNacimiento || t("booking.passengers.noBirthDate", "Sin fecha de nacimiento");
        main.append(name, meta);

        const controls = document.createElement("div");
        controls.className = "passenger-option-controls";
        const type = document.createElement("select");
        type.className = "wp-select passenger-type";
        type.disabled = true;
        type.setAttribute("aria-label", `${t("booking.passengers.typeFor", "Categoría de")} ${passenger.nombres}`);
        [
            ["ADULTO", t("booking.passengers.adult", "Adulto")],
            ["NINO", t("booking.passengers.child", "Niño")],
            ["INFANTE", t("booking.passengers.infant", "Infante")],
            ["ADULTO_MAYOR", t("booking.passengers.senior", "Adulto mayor")]
        ].forEach(([value, label]) => type.add(new Option(label, value)));

        const holderLabel = document.createElement("label");
        holderLabel.className = "passenger-holder";
        const holder = document.createElement("input");
        holder.type = "radio";
        holder.name = "passengerHolder";
        holder.className = "passenger-holder-input";
        holder.disabled = true;
        holderLabel.append(holder, document.createTextNode(t("booking.passengers.holder", "Titular de la reserva")));
        const saved = selection.get(String(passenger.pasajeroId));
        if (saved) {
            checkbox.checked = true;
            row.classList.add("is-selected");
            type.disabled = false;
            type.value = saved.tipoPasajero;
            holder.disabled = false;
            holder.checked = saved.esTitular;
        }
        controls.append(type, holderLabel);
        row.append(checkbox, main, controls);
        list.append(row);

        checkbox.addEventListener("change", () => {
            row.classList.toggle("is-selected", checkbox.checked);
            type.disabled = !checkbox.checked;
            holder.disabled = !checkbox.checked;
            if (!checkbox.checked) holder.checked = false;
            updateTotal();
        });
        type.addEventListener("change", updateTotal);
    });
}

function currentPassengerSelection() {
    return new Map([...document.querySelectorAll(".passenger-option")]
        .filter(row => row.querySelector(".passenger-select")?.checked)
        .map(row => [String(row.dataset.passengerId), {
            tipoPasajero: row.querySelector(".passenger-type").value,
            esTitular: row.querySelector(".passenger-holder-input").checked
        }]));
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
    button.textContent = t("booking.create.loading", "Creando reserva...");
    try {
        const passengers = ensurePassengers();
        await ensureClientProfile();
        const reserva = await api("/api/reservas", {
            method: "POST",
            body: {
                salidaId: Number(state.salida.salidaId),
                tarifaId: Number(state.tarifa.tarifaId),
                pasajeros: passengers,
                observaciones: value("resObservaciones")
            }
        });
        localStorage.setItem("wp_reserva_id", reserva.reservaId);
        localStorage.setItem("wp_codigo_reserva", reserva.codigoReserva);
        localStorage.setItem("wp_total_pagar", money(reserva.total));
        localStorage.setItem("wp_detalle_pasajeros", `${passengers.length} ${t("booking.passengers.travelers", "pasajeros")}`);
        localStorage.setItem("totalReserva", String(reserva.total));
        localStorage.setItem("fechaReserva", state.salida.fechaInicio);
        window.location.href = appUrl("pago.html");
    } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
            window.location.href = appUrl("login.html");
            return;
        }
        const fallback = t("booking.create.error", "No se pudo crear la reserva.");
        showMessage(error instanceof ApiError ? error.message : fallback, true);
    } finally {
        button.disabled = false;
        button.innerHTML = `<span data-i18n="booking.continue">${escapeHtml(t("booking.continue", "Continuar al pago"))}</span> <i class="fa-solid fa-arrow-right"></i>`;
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

function ensurePassengers() {
    const selected = [...document.querySelectorAll(".passenger-option")]
        .filter(row => row.querySelector(".passenger-select")?.checked)
        .map(row => ({
            pasajeroId: Number(row.dataset.passengerId),
            tipoPasajero: row.querySelector(".passenger-type").value,
            esTitular: row.querySelector(".passenger-holder-input").checked
        }));
    if (!selected.length) {
        throw new ApiError(t("booking.passengers.required", "Selecciona al menos un pasajero para continuar."), 400);
    }
    if (selected.length > 20) {
        throw new ApiError(t("booking.passengers.maximum", "Puedes seleccionar hasta 20 pasajeros."), 400);
    }
    if (selected.filter(passenger => passenger.esTitular).length !== 1) {
        throw new ApiError(t("booking.passengers.holderRequired", "Elige exactamente un titular de la reserva."), 400);
    }
    return selected;
}

function isProfileComplete() {
    return Boolean(value("resNombres") && value("resApellidos") && value("resTel"));
}

function updateTotal() {
    const selectedRows = [...document.querySelectorAll(".passenger-option")]
        .filter(row => row.querySelector(".passenger-select")?.checked);
    const count = selectedRows.length;
    const childPrice = Number(state.tarifa?.precioNino ?? state.precio);
    const estimatedTotal = selectedRows.reduce((total, row) => {
        const type = row.querySelector(".passenger-type").value;
        return total + (type === "NINO" || type === "INFANTE" ? childPrice : state.precio);
    }, 0);
    text("resPasajerosCount", String(count));
    text("sumPasajerosNum", String(count));
    text("sumTotal", money(estimatedTotal));
}

window.addEventListener("waypath:languagechange", () => {
    if (!state.pasajeros.length) return;
    const selection = currentPassengerSelection();
    renderPassengerPicker(selection);
    updateTotal();
});

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
