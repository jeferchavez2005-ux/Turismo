import { api } from "./api.js";
import { errorMessageWithTrace, t } from "./frontend-utils.js";
import { getDestinationImage } from "./destination-images.js";

document.addEventListener("DOMContentLoaded", loadReservation);

async function loadReservation() {
    const reservationId = Number(localStorage.getItem("wp_reserva_id") || 0);
    if (!reservationId) return showError(t("payment.errors.noReservation", "No se encontró una reserva válida para pagar."));
    try {
        const [detail, destinations] = await Promise.all([
            api(`/api/reservas/me/${reservationId}`),
            api("/api/catalogo/destinos")
        ]);
        text("sumDestino", detail.destino);
        text("sumFecha", formatRange(detail.fechaInicio, detail.fechaFin));
        text("sumTotal", money(detail.total));
        const image = document.getElementById("sumImg");
        image.src = getDestinationImage({ destino: detail.destino }, destinations);
        image.alt = detail.destino;
    } catch (error) {
        showError(errorMessageWithTrace(error, t("payment.errors.load", "No se pudo cargar el resumen de la reserva.")));
    }
}

function showError(message) {
    const panel = document.querySelector(".payment-unavailable");
    if (!panel) return;
    const alert = document.createElement("div");
    alert.className = "wp-alert wp-alert-danger";
    alert.setAttribute("role", "alert");
    alert.textContent = message;
    panel.prepend(alert);
}

function formatRange(start, end) {
    const locale = document.documentElement.lang || "es-PE";
    const formatter = new Intl.DateTimeFormat(locale, { dateStyle: "medium" });
    return `${formatter.format(new Date(`${start}T00:00:00`))} – ${formatter.format(new Date(`${end}T00:00:00`))}`;
}

function money(value) { return Number(value || 0).toLocaleString(document.documentElement.lang || "es-PE", { style: "currency", currency: "PEN" }); }
function text(id, value) { const node = document.getElementById(id); if (node) node.textContent = value; }
