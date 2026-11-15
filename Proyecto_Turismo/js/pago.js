import { ApiError, api, appUrl } from "./api.js";
import { errorMessageWithTrace, t } from "./frontend-utils.js";

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
        const fallback = t("payment.errors.load", "No se pudo cargar el resumen de la reserva.");
        showAlert(t("payment.errors.loadTitle", "No se pudo cargar la reserva"), errorMessageWithTrace(error, fallback));
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
    if (!state.reservaId) throw new ApiError(t("payment.errors.noReservation", "No se encontró una reserva válida para pagar."), 400);
    const detail = await api(`/api/reservas/me/${encodeURIComponent(state.reservaId)}`);
    text("sumDestino", detail.destino);
    text("sumFecha", detail.fechaInicio);
    text("sumTotal", money(detail.total));
    localStorage.setItem("totalReserva", String(detail.total));
    localStorage.setItem("wp_codigo_reserva", detail.codigoReserva);
    state.methods = await api("/api/pagos/metodos");
}

async function submitPayment(event) {
    event.preventDefault();
    const method = selectedMethod();
    if (!method) {
        showAlert("Metodo no disponible", "Seleccione un metodo de pago activo.");
        return;
    }
    const validationMessage = validatePaymentInput(method);
    if (validationMessage) {
        showAlert(t("payment.errors.invalidTitle", "Revisa los datos de pago"), validationMessage);
        return;
    }
    const button = event.submitter || document.querySelector("#formPago button[type='submit']");
    button.disabled = true;
    try {
        state.order = await api("/api/pagos/ordenes", {
            method: "POST",
            body: { reservaId: Number(state.reservaId) }
        });
        text("sumTotal", money(state.order.montoTotal));
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
        showAlert(t("payment.errors.processTitle", "No se pudo procesar el pago"),
            errorMessageWithTrace(error, t("payment.errors.process", "Intenta nuevamente.")));
    } finally {
        button.disabled = false;
        button.innerHTML = `<span data-i18n="payment.confirm">${t("payment.confirm", "Confirmar y pagar")}</span> <i class="fa-solid fa-lock" aria-hidden="true"></i>`;
    }
}

function validatePaymentInput(method) {
    if (method.codigo !== "TARJETA_CREDITO") return "";
    const number = document.getElementById("cardNumber")?.value.replace(/\s/g, "") || "";
    const expiry = document.getElementById("cardExpiry")?.value.trim() || "";
    const cvv = document.getElementById("cardCvv")?.value.trim() || "";
    const holder = document.getElementById("cardHolder")?.value.trim() || "";
    if (!/^\d{13,19}$/.test(number)) return t("payment.validation.cardNumber", "Ingresa un número de tarjeta válido.");
    if (!/^(0[1-9]|1[0-2])\/\d{2}$/.test(expiry)) return t("payment.validation.expiry", "Ingresa el vencimiento en formato MM/AA.");
    if (!/^\d{3,4}$/.test(cvv)) return t("payment.validation.cvv", "Ingresa un CVV válido.");
    if (holder.length < 3) return t("payment.validation.holder", "Ingresa el nombre del titular.");
    return "";
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
