import { ApiError, api, appUrl } from "./api.js";

const state = {
    reservaId: localStorage.getItem("wp_reserva_id"),
    order: null,
    methods: []
};

document.addEventListener("DOMContentLoaded", async () => {
    bindStaticSummary();
    document.getElementById("formPago")?.addEventListener("submit", submitPayment);
    try {
        await loadPaymentContext();
    } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
            window.location.href = appUrl("login.html");
            return;
        }
        showAlert("No se pudo cargar el pago", error instanceof ApiError ? error.message : "Intenta nuevamente.");
    }
});

function bindStaticSummary() {
    const destino = localStorage.getItem("destinoSeleccionado") || "Destino Desconocido";
    const total = Number.parseFloat(localStorage.getItem("totalReserva") || "0");
    const fecha = localStorage.getItem("fechaReserva") || "Fecha no especificada";
    const img = localStorage.getItem("imgSeleccionada") || "Img/machupichu.jpg";
    text("sumDestino", destino);
    text("sumTotal", money(total));
    text("sumFecha", fecha);
    const image = document.getElementById("sumImg");
    if (image) {
        image.src = img;
        image.alt = destino;
    }
}

async function loadPaymentContext() {
    if (!state.reservaId) throw new ApiError("No existe una reserva pendiente para pagar.", 400);
    const detail = await api(`/api/reservas/me/${encodeURIComponent(state.reservaId)}`);
    text("sumDestino", detail.destino);
    text("sumFecha", detail.fechaInicio);
    text("sumTotal", money(detail.total));
    localStorage.setItem("totalReserva", String(detail.total));
    localStorage.setItem("wp_codigo_reserva", detail.codigoReserva);
    state.methods = await api("/api/pagos/metodos");
    state.order = await api("/api/pagos/ordenes", {
        method: "POST",
        body: { reservaId: Number(state.reservaId) }
    });
    text("sumTotal", money(state.order.montoTotal));
}

async function submitPayment(event) {
    event.preventDefault();
    if (!state.order) {
        showAlert("Orden no disponible", "No se pudo preparar la orden de pago.");
        return;
    }
    const method = selectedMethod();
    if (!method) {
        showAlert("Metodo no disponible", "Seleccione un metodo de pago activo.");
        return;
    }
    const button = event.submitter || document.querySelector("#formPago button[type='submit']");
    button.disabled = true;
    try {
        Swal.fire({
            title: "Procesando pago...",
            html: 'Conectando con la pasarela simulada de WayPath<br><br><i class="fa-solid fa-circle-notch fa-spin fa-3x" style="color:#f97316;"></i>',
            showConfirmButton: false,
            allowOutsideClick: false
        });
        const result = localStorage.getItem("wp_pago_simulado") || "APROBADO";
        const payment = await api("/api/pagos/intentos", {
            method: "POST",
            body: {
                ordenPagoId: Number(state.order.ordenPagoId),
                metodoPagoId: method.metodoPagoId,
                resultadoSimulado: result,
                mensaje: `Resultado simulado desde checkout web: ${result}`
            }
        });
        await Swal.fire({
            title: payment.estado === "APROBADO" ? "Pago exitoso" : "Pago registrado",
            text: payment.estado === "APROBADO"
                ? "Tu reserva fue confirmada correctamente."
                : "El pago no fue aprobado. Puedes intentar con otro metodo.",
            icon: payment.estado === "APROBADO" ? "success" : "warning",
            confirmButtonColor: "#f97316",
            confirmButtonText: payment.estado === "APROBADO" ? "Ver mis compras" : "Intentar de nuevo"
        });
        if (payment.estado === "APROBADO") {
            localStorage.setItem("wp_pago_exitoso", "true");
            localStorage.setItem("wp_pago_id", payment.pagoId);
            window.location.href = appUrl("compras.html");
        }
    } catch (error) {
        showAlert("No se pudo procesar el pago", error instanceof ApiError ? error.message : "Intenta nuevamente.");
    } finally {
        button.disabled = false;
        button.innerHTML = 'Confirmar y pagar <i class="fa-solid fa-lock" aria-hidden="true"></i>';
    }
}

function selectedMethod() {
    const selected = document.querySelector("input[name='metodo']:checked")?.value || "tarjeta";
    const code = selected === "yape" ? "YAPE" : "TARJETA_CREDITO";
    return state.methods.find(x => x.codigo === code) || state.methods[0];
}

function showAlert(title, text) {
    if (window.Swal) {
        Swal.fire({ title, text, icon: "error", confirmButtonColor: "#f97316" });
    } else {
        alert(`${title}: ${text}`);
    }
}

function text(id, content) {
    const node = document.getElementById(id);
    if (node) node.textContent = content;
}

function money(value) {
    return `S/ ${Number(value || 0).toFixed(2)}`;
}
