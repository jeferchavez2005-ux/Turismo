import { api, ApiError } from "./api.js";

let reviews = [];

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("filtroDestino")?.addEventListener("change", renderReviews);
    document.getElementById("filtroEstrellas")?.addEventListener("change", renderReviews);
    document.getElementById("btnEscribirReseña")?.addEventListener("click", writeReview);
    loadReviews();
});

async function loadReviews() {
    const list = document.getElementById("reviewsList");
    if (!list) return;
    list.innerHTML = empty("Cargando resenas verificadas...");
    try {
        reviews = await api("/api/ventas/resenas");
        fillDestinationFilter(reviews);
        renderReviews();
    } catch (error) {
        list.innerHTML = empty(error.message || "No se pudieron cargar las resenas.");
    }
}

function renderReviews() {
    const list = document.getElementById("reviewsList");
    const destination = document.getElementById("filtroDestino")?.value || "";
    const starsFilter = document.getElementById("filtroEstrellas")?.value || "";
    const filtered = reviews.filter(item =>
        (!destination || item.destino === destination) &&
        (!starsFilter || String(item.calificacion) === starsFilter));
    if (filtered.length === 0) {
        list.innerHTML = empty("Aun no hay resenas publicadas con esos filtros.");
        return;
    }
    list.innerHTML = filtered.map(reviewHtml).join("");
}

async function writeReview() {
    try {
        const reservas = await api("/api/reservas/me");
        const available = reservas.filter(x => (x.estado || "").toUpperCase() === "COMPLETADA");
        if (available.length === 0) {
            await Swal.fire("Sin viajes completados", "Solo puedes resenar reservas completadas.", "info");
            return;
        }
        const options = available.map(x =>
            `<option value="${x.reservaId}">${escapeHtml(x.codigoReserva)} - ${escapeHtml(x.paquete)}</option>`).join("");
        const result = await Swal.fire({
            title: "Comparte tu experiencia",
            html: `
                <select id="swal-reserva" class="swal2-input">${options}</select>
                <select id="swal-stars" class="swal2-input">
                    <option value="5">5 estrellas</option>
                    <option value="4">4 estrellas</option>
                    <option value="3">3 estrellas</option>
                    <option value="2">2 estrellas</option>
                    <option value="1">1 estrella</option>
                </select>
                <input id="swal-title" class="swal2-input" maxlength="160" placeholder="Titulo">
                <textarea id="swal-text" class="swal2-textarea" maxlength="2000" placeholder="Escribe tu resena"></textarea>
            `,
            showCancelButton: true,
            confirmButtonText: "Enviar",
            confirmButtonColor: "#f97316",
            cancelButtonText: "Cancelar",
            preConfirm: () => {
                const reservaId = Number(document.getElementById("swal-reserva").value);
                const calificacion = Number(document.getElementById("swal-stars").value);
                const titulo = document.getElementById("swal-title").value.trim();
                const comentario = document.getElementById("swal-text").value.trim();
                if (!comentario) {
                    Swal.showValidationMessage("Escribe tu resena.");
                    return false;
                }
                return { reservaId, calificacion, titulo, comentario };
            }
        });
        if (!result.isConfirmed) return;
        await api("/api/ventas/resenas", { method: "POST", body: result.value });
        await Swal.fire("Recibida", "Tu resena fue enviada para moderacion.", "success");
        await loadReviews();
    } catch (error) {
        const message = error instanceof ApiError && error.status === 401
            ? "Inicia sesion para escribir una resena."
            : error.message || "No se pudo registrar la resena.";
        await Swal.fire("WayPath", message, "warning");
    }
}

function reviewHtml(item) {
    return `
        <article class="review-card" data-destino="${escapeHtml(item.destino)}" data-stars="${item.calificacion}">
            <img src="${imageFor(item.destino)}" alt="${escapeHtml(item.destino)}" class="review-img" loading="lazy">
            <div class="review-body">
                <div class="review-header">
                    <div class="reviewer-info">
                        <div class="avatar">${initials(item.cliente)}</div>
                        <div>
                            <h4>${escapeHtml(item.cliente)}</h4>
                            <span class="reviewer-loc"><i class="fa-solid fa-circle-check"></i> Cliente verificado</span>
                        </div>
                    </div>
                    <div class="review-stars">${stars(item.calificacion)}</div>
                </div>
                <h5 class="review-title">${escapeHtml(item.titulo || item.destino)}</h5>
                <p class="review-text">${escapeHtml(item.comentario)}</p>
                <div class="review-meta">
                    <span class="review-dest"><i class="fa-solid fa-map-location-dot"></i> ${escapeHtml(item.destino)}</span>
                    <span class="review-date"><i class="fa-regular fa-calendar"></i> ${formatDate(item.creadoEn)}</span>
                </div>
            </div>
        </article>`;
}

function fillDestinationFilter(items) {
    const select = document.getElementById("filtroDestino");
    if (!select) return;
    const current = select.value;
    const names = [...new Set(items.map(x => x.destino).filter(Boolean))].sort();
    select.innerHTML = `<option value="">Todos los destinos</option>${names.map(x => `<option>${escapeHtml(x)}</option>`).join("")}`;
    select.value = current;
}

function stars(score) {
    return Array.from({ length: 5 }, (_, index) =>
        `<i class="${index < score ? "fa-solid" : "fa-regular"} fa-star"></i>`).join("");
}

function initials(name) {
    return (name || "WP").split(/\s+/).slice(0, 2).map(x => x[0]).join("").toUpperCase();
}

function imageFor(destination) {
    const normalized = (destination || "").toLowerCase();
    if (normalized.includes("titicaca")) return "Img/LAGOTITICACA.webp";
    if (normalized.includes("mancora") || normalized.includes("máncora")) return "Img/MANCORA.jpg";
    if (normalized.includes("huacachina")) return "Img/OASISDEHUACACHINA.jpg";
    return "Img/machupichu.jpg";
}

function formatDate(value) {
    return new Date(value).toLocaleDateString("es-PE", { month: "short", year: "numeric" });
}

function empty(text) {
    return `<div class="wp-empty-state"><i class="fa-solid fa-star" aria-hidden="true"></i><strong>${escapeHtml(text)}</strong><p>Las resenas aprobadas apareceran aqui cuando existan en WayPathDB.</p></div>`;
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, char => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    })[char]);
}
