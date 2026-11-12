import { api, ApiError } from "./api.js";

let requestVersion = 0;
const t = (key, fallback) => {
    const value = window.WayPathI18n?.t(key);
    return value && value !== key ? value : fallback;
};

document.addEventListener("DOMContentLoaded", async () => {
    document.getElementById("filtroDestino")?.addEventListener("change", loadReviews);
    document.getElementById("filtroEstrellas")?.addEventListener("change", loadReviews);
    document.getElementById("btnEscribirReseña")?.addEventListener("click", writeReview);
    await Promise.allSettled([loadDestinations(), loadReviews()]);
});
window.addEventListener("waypath:languagechange", loadReviews);

async function loadDestinations() {
    const select = document.getElementById("filtroDestino");
    if (!select) return;
    const destinations = await api("/api/catalogo/destinos");
    const current = select.value;
    select.innerHTML = `<option value="">${escapeHtml(t("reviews.filters.allDestinations", "Todos los destinos"))}</option>` +
        destinations.map(item => `<option value="${escapeHtml(item.destinoId)}">${escapeHtml(item.nombre)}</option>`).join("");
    select.value = current;
}

async function loadReviews() {
    const list = document.getElementById("reviewsList");
    if (!list) return;
    const version = ++requestVersion;
    list.innerHTML = empty(t("reviews.loading", "Cargando reseñas verificadas..."));
    const query = new URLSearchParams();
    const destination = document.getElementById("filtroDestino")?.value;
    const starsFilter = document.getElementById("filtroEstrellas")?.value;
    if (destination) query.set("destinoId", destination);
    if (starsFilter) query.set("calificacion", starsFilter);
    try {
        const reviews = await api(`/api/ventas/resenas${query.size ? `?${query}` : ""}`);
        if (version !== requestVersion) return;
        list.innerHTML = reviews.length ? reviews.map(reviewHtml).join("") :
            empty(t("reviews.empty.filtered", "Aún no hay reseñas publicadas con esos filtros."),
                t("reviews.empty.help", "Prueba otra combinación de destino y calificación."));
    } catch (error) {
        if (version !== requestVersion) return;
        const trace = error.traceId ? `${t("errors.trace", "Código de diagnóstico")}: ${error.traceId}` : "";
        list.innerHTML = empty(error.message || t("reviews.loadError", "No se pudieron cargar las reseñas."), trace);
    }
}

async function writeReview() {
    try {
        const reservas = await api("/api/reservas/me");
        const available = reservas.filter(item => (item.estado || "").toUpperCase() === "COMPLETADA");
        if (!available.length) {
            await Swal.fire(t("reviews.ineligible.title", "Sin viajes completados"),
                t("reviews.ineligible.message", "Solo puedes publicar una reseña después de completar el viaje."), "info");
            return;
        }
        const options = available.map(item => `<option value="${item.reservaId}">${escapeHtml(item.codigoReserva)} - ${escapeHtml(item.paquete)}</option>`).join("");
        const result = await Swal.fire({
            title: t("reviews.form.title", "Comparte tu experiencia"),
            html: `<select id="swal-reserva" class="swal2-input">${options}</select>
                <select id="swal-stars" class="swal2-input">${[5,4,3,2,1].map(value => `<option value="${value}">${value} ${escapeHtml(t("reviews.form.stars", "estrellas"))}</option>`).join("")}</select>
                <input id="swal-title" class="swal2-input" maxlength="160" placeholder="${escapeHtml(t("reviews.form.reviewTitle", "Título"))}">
                <textarea id="swal-text" class="swal2-textarea" maxlength="2000" placeholder="${escapeHtml(t("reviews.form.comment", "Escribe tu reseña"))}"></textarea>`,
            showCancelButton: true, confirmButtonText: t("common.send", "Enviar"),
            cancelButtonText: t("common.cancel", "Cancelar"), confirmButtonColor: "#f97316",
            preConfirm: () => {
                const comentario = document.getElementById("swal-text").value.trim();
                if (!comentario) { Swal.showValidationMessage(t("reviews.form.commentRequired", "Escribe tu reseña.")); return false; }
                return { reservaId: Number(document.getElementById("swal-reserva").value),
                    calificacion: Number(document.getElementById("swal-stars").value),
                    titulo: document.getElementById("swal-title").value.trim(), comentario };
            }
        });
        if (!result.isConfirmed) return;
        await api("/api/ventas/resenas", { method: "POST", body: result.value });
        await Swal.fire(t("reviews.success.title", "Recibida"),
            t("reviews.success.message", "Tu reseña fue enviada para moderación."), "success");
        await loadReviews();
    } catch (error) {
        const byStatus = { 0: t("errors.network", "No pudimos conectar con WayPath. Inténtalo nuevamente."),
            401: t("reviews.errors.authentication", "Inicia sesión para escribir una reseña."),
            403: t("reviews.errors.permission", "No tienes permiso para publicar esta reseña."), 409: error.message };
        const trace = error.traceId ? `\n${t("errors.trace", "Código de diagnóstico")}: ${error.traceId}` : "";
        await Swal.fire("WayPath", `${byStatus[error.status] || error.message || t("reviews.errors.internal", "No se pudo registrar la reseña.")}${trace}`, "warning");
    }
}

function reviewHtml(item) {
    return `<article class="review-card" data-destino-id="${escapeHtml(item.destinoId)}" data-stars="${item.calificacion}">
        <img src="${imageFor(item.destino)}" alt="${escapeHtml(item.destino)}" class="review-img" loading="lazy"><div class="review-body">
        <div class="review-header"><div class="reviewer-info"><div class="avatar">${initials(item.cliente)}</div><div><h4>${escapeHtml(item.cliente)}</h4>
        <span class="reviewer-loc"><i class="fa-solid fa-circle-check"></i> ${escapeHtml(t("reviews.verified", "Cliente verificado"))}</span></div></div>
        <div class="review-stars">${stars(item.calificacion)}</div></div><h5 class="review-title">${escapeHtml(item.titulo || item.destino)}</h5>
        <p class="review-text">${escapeHtml(item.comentario)}</p><div class="review-meta"><span class="review-dest"><i class="fa-solid fa-map-location-dot"></i> ${escapeHtml(item.destino)}</span>
        <span class="review-date"><i class="fa-regular fa-calendar"></i> ${formatDate(item.creadoEn)}</span></div></div></article>`;
}
function stars(score) { return Array.from({ length: 5 }, (_, i) => `<i class="${i < score ? "fa-solid" : "fa-regular"} fa-star"></i>`).join(""); }
function initials(name) { return (name || "WP").split(/\s+/).slice(0, 2).map(x => x[0]).join("").toUpperCase(); }
function imageFor(name) { const value = (name || "").toLowerCase(); if (value.includes("titicaca")) return "Img/LAGOTITICACA.webp"; if (value.includes("mancora") || value.includes("máncora")) return "Img/MANCORA.jpg"; if (value.includes("huacachina")) return "Img/OASISDEHUACACHINA.jpg"; return "Img/machupichu.jpg"; }
function formatDate(value) { return new Intl.DateTimeFormat(document.documentElement.lang || "es", { month: "short", year: "numeric" }).format(new Date(value)); }
function empty(title, detail = "") { return `<div class="wp-empty-state"><i class="fa-solid fa-star" aria-hidden="true"></i><strong>${escapeHtml(title)}</strong>${detail ? `<p>${escapeHtml(detail)}</p>` : ""}</div>`; }
function escapeHtml(value) { return String(value ?? "").replace(/[&<>"']/g, c => ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", "\"":"&quot;", "'":"&#39;" })[c]); }
