import { ApiError, api } from "./api.js";

const state = {
    destinoId: localStorage.getItem("destinoIdSeleccionado"),
    destino: localStorage.getItem("destinoSeleccionado") || "Destino turistico",
    imagen: localStorage.getItem("imgSeleccionada") || "Img/machupichu.jpg",
    paquete: null,
    salida: null,
    tarifa: null,
    precio: Number.parseFloat(localStorage.getItem("precioSeleccionado") || "0") || 0
};

document.addEventListener("DOMContentLoaded", async () => {
    bindSummary();
    document.getElementById("resPasajeros")?.addEventListener("change", updateTotal);
    document.getElementById("formReserva")?.addEventListener("submit", submitReservation);
    try {
        await loadReservationContext();
        await loadProfile();
        updateTotal();
    } catch (error) {
        showMessage(error instanceof ApiError ? error.message : "No se pudo cargar la informacion de reserva.", true);
    }
});

function bindSummary() {
    text("sumDestino", state.destino);
    const image = document.getElementById("sumImg");
    if (image) {
        image.src = state.imagen;
        image.alt = state.destino;
    }
    text("sumPrecioUnitario", money(state.precio));
    updateTotal();
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
    const me = await api("/api/auth/me");
    const client = await api("/api/clientes/me");
    document.getElementById("resEmail").value = me.email || "";
    if (client?.existe && client.perfil) {
        document.getElementById("resNombres").value = client.perfil.nombres || "";
        document.getElementById("resApellidos").value = client.perfil.apellidos || "";
        document.getElementById("resTel").value = client.perfil.telefono || "";
    }
}

async function submitReservation(event) {
    event.preventDefault();
    clearMessage();
    if (!state.salida || !state.tarifa) {
        showMessage("No hay una salida disponible para reservar.", true);
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
    const selected = existing.slice(0, count).map(x => x.pasajeroId);
    while (selected.length < count) {
        const index = selected.length;
        const passenger = await api("/api/reservas/me/pasajeros", {
            method: "POST",
            body: {
                nombres: index === 0 ? value("resNombres") : `Acompanante ${index + 1}`,
                apellidos: index === 0 ? value("resApellidos") : value("resApellidos"),
                fechaNacimiento: null,
                requerimientosEspeciales: value("resObservaciones")
            }
        });
        selected.push(passenger.pasajeroId);
    }
    return selected;
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
