import { ApiError, api } from "./api.js";

const grid = document.getElementById("destinosGrid");
const emptyState = document.getElementById("emptyState");
const buscador = document.getElementById("buscador-destinos");
const filtroPrecio = document.getElementById("filtro-precio");
let destinos = [];

function t(key) {
    return window.WayPathI18n?.t(key) || {
        "destinations.from": "Desde",
        "destinations.featured": "Destacado",
        "common.reserve": "Reservar"
    }[key] || key;
}

function money(value, currency) {
    if (value === null || value === undefined) return "Consultar";
    const prefix = currency === "USD" ? "$" : "S/";
    return `${prefix} ${Number(value).toLocaleString("es-PE", { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
}
function fallbackImage(url) {
    return url && url.length ? url : "Img/machupichu.jpg";
}
function card(destino) {
    const article = document.createElement("article");
    article.className = "dest-card";
    article.dataset.nombre = destino.nombre;
    article.dataset.precio = destino.precioDesde ?? "";

    const imgWrap = document.createElement("div");
    imgWrap.className = "dest-img-wrap";
    const img = document.createElement("img");
    img.src = fallbackImage(destino.imagenUrl);
    img.alt = destino.imagenAlt || destino.nombre;
    img.loading = "lazy";
    const badge = document.createElement("span");
    badge.className = destino.esDestacado ? "dest-badge-img dest-badge-popular" : "dest-badge-img";
    const badgeIcon = document.createElement("i");
    badgeIcon.className = destino.esDestacado ? "fa-solid fa-fire" : "fa-solid fa-location-dot";
    badge.append(badgeIcon, document.createTextNode(` ${destino.esDestacado ? t("destinations.featured") : destino.region}`));
    imgWrap.append(img, badge);

    const body = document.createElement("div");
    body.className = "dest-body";
    const title = document.createElement("h3");
    title.textContent = destino.nombre;
    const location = document.createElement("p");
    location.className = "location";
    const locationIcon = document.createElement("i");
    locationIcon.className = "fa-solid fa-location-dot";
    location.append(locationIcon, document.createTextNode(` ${destino.ciudad}, ${destino.pais}`));
    const desc = document.createElement("p");
    desc.className = "desc";
    desc.textContent = destino.descripcionCorta;

    const footer = document.createElement("div");
    footer.className = "dest-footer";
    const priceInfo = document.createElement("div");
    priceInfo.className = "price-info";
    const from = document.createElement("span");
    from.className = "price-from";
    from.textContent = t("destinations.from");
    const amount = document.createElement("span");
    amount.className = "price-amount";
    amount.textContent = money(destino.precioDesde, destino.moneda);
    priceInfo.append(from, amount);

    const reserve = document.createElement("a");
    reserve.href = "reserva.html";
    reserve.className = "wp-btn wp-btn-primary btn-reserva";
    reserve.dataset.destinoId = destino.destinoId;
    reserve.dataset.nombre = destino.nombre;
    reserve.dataset.precio = destino.precioDesde ?? "0";
    reserve.dataset.img = fallbackImage(destino.imagenUrl);
    const calendar = document.createElement("i");
    calendar.className = "fa-solid fa-calendar-plus";
    reserve.append(calendar, document.createTextNode(` ${t("common.reserve")}`));
    reserve.addEventListener("click", event => handleReserveClick(event, destino));

    footer.append(priceInfo, reserve);
    body.append(title, location, desc, footer);
    article.append(imgWrap, body);
    return article;
}

async function handleReserveClick(event, destino) {
    event.preventDefault();
    saveReservationIntent(destino);
    try {
        const user = await api("/api/auth/me");
        if (!(user.roles || []).includes("CLIENTE")) {
            await notice("Acceso de cliente requerido", "Debes ingresar con una cuenta Cliente para realizar una reserva.");
            return;
        }
        const client = await api("/api/clientes/me");
        if (!isProfileComplete(client?.perfil)) {
            await goTo("Completa tu perfil", "Antes de reservar necesitamos tus nombres, apellidos y teléfono.", "cuenta.html?returnUrl=reserva.html");
            return;
        }
        window.location.href = "reserva.html";
    } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
            await authPrompt();
            return;
        }
        await notice("No se pudo iniciar la reserva", error.message || "Intenta nuevamente.");
    }
}

function saveReservationIntent(destino) {
    localStorage.setItem("destinoIdSeleccionado", destino.destinoId);
    localStorage.setItem("destinoSeleccionado", destino.nombre);
    localStorage.setItem("precioSeleccionado", destino.precioDesde ?? "0");
    localStorage.setItem("imgSeleccionada", fallbackImage(destino.imagenUrl));
    localStorage.setItem("wp_auth_return_url", "/reserva.html");
}

function isProfileComplete(profile) {
    return Boolean(profile?.nombres?.trim() && profile?.apellidos?.trim() && profile?.telefono?.trim());
}

async function authPrompt() {
    const loginUrl = "login.html?returnUrl=%2Freserva.html";
    const registerUrl = "registro.html?returnUrl=%2Freserva.html";
    if (!window.Swal) {
        window.location.href = window.confirm("Debes iniciar sesión para realizar una reserva.\n\nAceptar: iniciar sesión\nCancelar: registrarse") ? loginUrl : registerUrl;
        return;
    }
    const result = await Swal.fire({
        title: "Debes iniciar sesión para realizar una reserva",
        text: "Puedes entrar con tu cuenta o crear una nueva para continuar con el destino seleccionado.",
        icon: "info",
        showCancelButton: true,
        confirmButtonText: "Iniciar sesión",
        cancelButtonText: "Registrarse",
        confirmButtonColor: "#f97316"
    });
    window.location.href = result.isConfirmed ? loginUrl : registerUrl;
}

async function goTo(title, text, url) {
    if (window.Swal) await Swal.fire({ title, text, icon: "warning", confirmButtonColor: "#f97316" });
    else alert(`${title}: ${text}`);
    window.location.href = url;
}

async function notice(title, text) {
    if (window.Swal) await Swal.fire({ title, text, icon: "warning", confirmButtonColor: "#f97316" });
    else alert(`${title}: ${text}`);
}
function render(rows) {
    grid.replaceChildren();
    rows.forEach(destino => grid.append(card(destino)));
    emptyState.style.display = rows.length ? "none" : "block";
}
function filterLocal() {
    const text = buscador.value.trim().toLowerCase();
    const [min, max] = (filtroPrecio.value || "0-999999").split("-").map(Number);
    const rows = destinos.filter(destino => {
        const price = Number(destino.precioDesde ?? 0);
        const searchable = `${destino.nombre} ${destino.descripcionCorta} ${destino.ciudad} ${destino.region}`.toLowerCase();
        return searchable.includes(text) && price >= min && price <= max;
    });
    render(rows);
}
async function loadDestinations() {
    try {
        destinos = await api("/api/catalogo/destinos");
        render(destinos);
    } catch {
        destinos = Array.from(document.querySelectorAll("#destinosGrid .dest-card")).map(card => ({
            nombre: card.dataset.nombre,
            precioDesde: Number(card.dataset.precio || 0),
            descripcionCorta: card.querySelector(".desc")?.textContent || "",
            ciudad: card.querySelector(".location")?.textContent?.replace(/\s+/g, " ").trim() || "",
            region: "",
            pais: "Peru"
        }));
        filterLocal();
    }
}

buscador.addEventListener("input", filterLocal);
filtroPrecio.addEventListener("change", filterLocal);
await loadDestinations();
window.addEventListener("waypath:languagechange", filterLocal);
