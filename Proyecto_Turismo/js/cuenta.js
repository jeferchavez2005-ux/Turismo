import { api, broadcastAuthChange } from "./api.js";

const message = document.getElementById("accountMessage");
const logout = document.getElementById("logout");
const state = { user: null, cliente: null, catalogos: null, documentos: [], direcciones: [], pasajeros: [] };

const t = (key, fallback) => {
    const translated = window.WayPathI18n?.t(key);
    return translated && translated !== key ? translated : fallback;
};

window.addEventListener("waypath:languagechange", () => renderPassengers());

function showMessage(text) { message.textContent = text || ""; }
function showError(error) {
    if (error.status === 401) { location.replace("/login.html"); return; }
    showMessage(error.message);
}
function value(id) { return document.getElementById(id).value.trim(); }
function nullable(id) { const data = value(id); return data.length ? data : null; }
function checked(id) { return document.getElementById(id).checked; }
function setValue(id, data) { document.getElementById(id).value = data ?? ""; }
function optionLabel(text, flag) { return flag ? `${text} - Principal` : text; }
function asIntOrNull(id) {
    const data = value(id);
    return data.length ? Number(data) : null;
}
function ensureClient() {
    if (!state.cliente) throw new Error("Completa primero tu perfil de cliente.");
}
function setBusy(form, busy) {
    const button = form.querySelector('button[type="submit"]');
    if (button) button.disabled = busy;
}
function setPassengersStatus(text, isError = false) {
    const status = document.getElementById("passengersStatus");
    status.textContent = text || "";
    status.classList.toggle("is-error", isError);
}
function fillSelect(id, rows, valueKey, label, emptyText = "Seleccionar") {
    const select = document.getElementById(id);
    select.replaceChildren(new Option(emptyText, ""));
    rows.forEach(row => select.add(new Option(label(row), row[valueKey])));
}
function item(container, title, details, actions) {
    const card = document.createElement("div");
    card.className = "account-item";
    const text = document.createElement("div");
    const strong = document.createElement("strong");
    strong.textContent = title;
    const span = document.createElement("span");
    span.className = "account-small";
    span.textContent = details;
    text.append(strong, span);
    const actionBox = document.createElement("div");
    actionBox.className = "account-item-actions";
    actions.forEach(action => actionBox.append(action));
    card.append(text, actionBox);
    container.append(card);
}
function actionButton(label, icon, handler) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "wp-btn wp-btn-outline";
    const i = document.createElement("i");
    i.className = `fa-solid ${icon}`;
    i.setAttribute("aria-hidden", "true");
    button.append(i, document.createTextNode(` ${label}`));
    button.addEventListener("click", handler);
    return button;
}

async function loadAccount() {
    state.user = await api("/api/auth/me");
    state.catalogos = await api("/api/clientes/catalogos");
    const response = await api("/api/clientes/me");
    state.cliente = response.perfil;
    renderUser();
    renderCatalogs();
    renderClient();
    document.getElementById("accountContent").hidden = false;
    logout.disabled = false;
    if (state.cliente) {
        await Promise.all([loadClientCollections(), loadPassengers()]);
    }
    else {
        showMessage("Completa tu perfil de cliente para habilitar documentos, direcciones, pasajeros y preferencias.");
        setPassengersStatus(t("account.passengers.profileRequired", "Completa primero tu perfil de cliente para registrar pasajeros."), true);
        document.querySelectorAll("#passengerForm input, #passengerForm textarea, #passengerForm button").forEach(control => { control.disabled = true; });
    }
}

function renderUser() {
    document.getElementById("accountEmail").textContent = state.user.email;
    setValue("name", state.user.nombre || "");
}
function renderCatalogs() {
    fillSelect("documentType", state.catalogos.tiposDocumento, "tipoDocumentoId",
        row => `${row.codigo} - ${row.nombre}`, "Tipo de documento");
    fillSelect("documentCountry", state.catalogos.paises, "paisId",
        row => row.nombre, "Pais");
    fillSelect("addressCity", state.catalogos.ciudades, "ciudadId",
        row => `${row.nombre}, ${row.region}`, "Ciudad");
    fillSelect("preferredCurrency", state.catalogos.monedas, "monedaId",
        row => `${row.codigoIso} - ${row.nombre}`, "Moneda");
}
function renderClient() {
    const p = state.cliente;
    if (!p) {
        const parts = (state.user.nombre || "").trim().split(/\s+/).filter(Boolean);
        setValue("clientNames", parts.slice(0, Math.max(1, parts.length - 1)).join(" "));
        setValue("clientLastNames", parts.length > 1 ? parts.at(-1) : "");
        return;
    }
    setValue("clientNames", p.nombres);
    setValue("clientLastNames", p.apellidos);
    setValue("clientPhone", p.telefono);
    setValue("clientBirthDate", p.fechaNacimiento);
    setValue("clientGender", p.genero);
}
async function loadClientCollections() {
    const [documentos, direcciones, preferencias] = await Promise.all([
        api("/api/clientes/me/documentos"),
        api("/api/clientes/me/direcciones"),
        api("/api/clientes/me/preferencias")
    ]);
    state.documentos = documentos;
    state.direcciones = direcciones;
    renderDocuments();
    renderAddresses();
    renderPreferences(preferencias);
    showMessage("");
}

async function loadPassengers() {
    setPassengersStatus(t("account.passengers.loading", "Cargando pasajeros..."));
    try {
        state.pasajeros = await api("/api/reservas/me/pasajeros");
        renderPassengers();
        setPassengersStatus("");
    } catch (error) {
        setPassengersStatus(error.message || t("account.passengers.loadError", "No pudimos cargar tus pasajeros. Inténtalo nuevamente."), true);
    }
}

function renderPassengers() {
    const box = document.getElementById("passengersList");
    box.replaceChildren();
    if (!state.pasajeros.length) {
        const empty = document.createElement("p");
        empty.className = "account-empty";
        empty.textContent = t("account.passengers.empty", "Aún no registras pasajeros. Agrega el primero para poder reservar.");
        box.append(empty);
        return;
    }
    state.pasajeros.forEach(passenger => {
        const birthDate = passenger.fechaNacimiento
            ? new Intl.DateTimeFormat(document.documentElement.lang || "es", { dateStyle: "medium" }).format(new Date(`${passenger.fechaNacimiento}T00:00:00`))
            : t("account.passengers.noBirthDate", "Fecha de nacimiento no indicada");
        const requirements = passenger.requerimientosEspeciales
            ? `${birthDate} · ${passenger.requerimientosEspeciales}`
            : birthDate;
        item(box, `${passenger.nombres} ${passenger.apellidos}`, requirements, []);
    });
}

function renderDocuments() {
    const box = document.getElementById("documentsList");
    box.replaceChildren();
    if (!state.documentos.length) {
        const empty = document.createElement("p");
        empty.className = "account-empty";
        empty.textContent = "Aun no registras documentos.";
        box.append(empty);
        return;
    }
    state.documentos.forEach(doc => item(box,
        optionLabel(`${doc.tipoDocumento}: ${doc.numeroDocumento}`, doc.esPrincipal),
        doc.paisEmision ? `Emitido en ${doc.paisEmision}` : "Pais de emision no indicado",
        [
            actionButton("Editar", "fa-pen", () => editDocument(doc)),
            actionButton("Eliminar", "fa-trash", () => deleteDocument(doc.documentoClienteId))
        ]));
}
function editDocument(doc) {
    setValue("documentId", doc.documentoClienteId);
    setValue("documentType", doc.tipoDocumentoId);
    setValue("documentNumber", doc.numeroDocumento);
    setValue("documentCountry", doc.paisEmisionId);
    setValue("documentExpiration", doc.fechaVencimiento);
    document.getElementById("documentMain").checked = doc.esPrincipal;
}
async function deleteDocument(id) {
    if (!confirm("Eliminar este documento?")) return;
    try {
        await api(`/api/clientes/me/documentos/${id}`, { method: "DELETE" });
        state.documentos = await api("/api/clientes/me/documentos");
        renderDocuments();
        showMessage("Documento eliminado.");
    } catch (error) { showError(error); }
}

function renderAddresses() {
    const box = document.getElementById("addressesList");
    box.replaceChildren();
    if (!state.direcciones.length) {
        const empty = document.createElement("p");
        empty.className = "account-empty";
        empty.textContent = "Aun no registras direcciones.";
        box.append(empty);
        return;
    }
    state.direcciones.forEach(dir => item(box,
        optionLabel(dir.lineaDireccion, dir.esPrincipal),
        `${dir.ciudad}, ${dir.region}, ${dir.pais}`,
        [
            actionButton("Editar", "fa-pen", () => editAddress(dir)),
            actionButton("Eliminar", "fa-trash", () => deleteAddress(dir.direccionClienteId))
        ]));
}
function editAddress(dir) {
    setValue("addressId", dir.direccionClienteId);
    setValue("addressCity", dir.ciudadId);
    setValue("addressLine", dir.lineaDireccion);
    setValue("addressPostalCode", dir.codigoPostal);
    document.getElementById("addressMain").checked = dir.esPrincipal;
}
async function deleteAddress(id) {
    if (!confirm("Eliminar esta direccion?")) return;
    try {
        await api(`/api/clientes/me/direcciones/${id}`, { method: "DELETE" });
        state.direcciones = await api("/api/clientes/me/direcciones");
        renderAddresses();
        showMessage("Direccion eliminada.");
    } catch (error) { showError(error); }
}
function renderPreferences(pref) {
    setValue("preferredLanguage", pref?.idiomaPreferido || "es");
    setValue("preferredCurrency", pref?.monedaPreferidaId || "");
    document.getElementById("receivePromotions").checked = pref?.recibePromociones ?? true;
    document.getElementById("requiresAccessibility").checked = pref?.requiereAccesibilidad ?? false;
    setValue("preferenceNotes", pref?.observaciones || "");
}

try { await loadAccount(); }
catch (error) { showError(error); }

logout.addEventListener("click", async () => {
    logout.disabled = true;
    try {
        await api("/api/auth/logout", { method: "POST" });
        broadcastAuthChange();
        location.replace("/login.html");
    } catch (error) { showError(error); }
    finally { logout.disabled = false; }
});

document.getElementById("profileForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    setBusy(form, true);
    try {
        await api("/api/auth/profile", { method: "PUT", body: { nombre: value("name") } });
        showMessage("Usuario actualizado.");
        broadcastAuthChange();
    } catch (error) { showError(error); }
    finally { setBusy(form, false); }
});

document.getElementById("clientProfileForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    setBusy(form, true);
    try {
        state.cliente = await api("/api/clientes/me", { method: "PUT", body: {
            nombres: value("clientNames"),
            apellidos: value("clientLastNames"),
            telefono: nullable("clientPhone"),
            fechaNacimiento: nullable("clientBirthDate"),
            genero: nullable("clientGender")
        } });
        document.querySelectorAll("#passengerForm input, #passengerForm textarea, #passengerForm button").forEach(control => { control.disabled = false; });
        await Promise.all([loadClientCollections(), loadPassengers()]);
        showMessage("Perfil de cliente actualizado.");
    } catch (error) { showError(error); }
    finally { setBusy(form, false); }
});

document.getElementById("passengerForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const birthDate = nullable("passengerBirthDate");
    const today = new Date().toISOString().slice(0, 10);
    if (birthDate && birthDate > today) {
        setPassengersStatus(t("account.passengers.futureBirthDate", "La fecha de nacimiento no puede estar en el futuro."), true);
        document.getElementById("passengerBirthDate").focus();
        return;
    }
    setBusy(form, true);
    setPassengersStatus(t("account.passengers.saving", "Guardando pasajero..."));
    try {
        ensureClient();
        await api("/api/reservas/me/pasajeros", { method: "POST", body: {
            nombres: value("passengerNames"),
            apellidos: value("passengerLastNames"),
            fechaNacimiento: birthDate,
            requerimientosEspeciales: nullable("passengerRequirements")
        } });
        form.reset();
        await loadPassengers();
        setPassengersStatus(t("account.passengers.saved", "Pasajero agregado correctamente."));
    } catch (error) {
        if (error.status === 401) { showError(error); return; }
        setPassengersStatus(error.message || t("account.passengers.saveError", "No pudimos guardar el pasajero."), true);
    } finally { setBusy(form, false); }
});

document.getElementById("passengerBirthDate").max = new Date().toISOString().slice(0, 10);

document.getElementById("documentForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    setBusy(form, true);
    try {
        ensureClient();
        const id = value("documentId");
        const method = id ? "PUT" : "POST";
        const path = id ? `/api/clientes/me/documentos/${id}` : "/api/clientes/me/documentos";
        await api(path, { method, body: {
            tipoDocumentoId: Number(value("documentType")),
            numeroDocumento: value("documentNumber"),
            paisEmisionId: asIntOrNull("documentCountry"),
            fechaVencimiento: nullable("documentExpiration"),
            esPrincipal: checked("documentMain")
        } });
        form.reset();
        state.documentos = await api("/api/clientes/me/documentos");
        renderDocuments();
        showMessage("Documento guardado.");
    } catch (error) { showError(error); }
    finally { setBusy(form, false); }
});

document.getElementById("addressForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    setBusy(form, true);
    try {
        ensureClient();
        const id = value("addressId");
        const method = id ? "PUT" : "POST";
        const path = id ? `/api/clientes/me/direcciones/${id}` : "/api/clientes/me/direcciones";
        await api(path, { method, body: {
            ciudadId: Number(value("addressCity")),
            lineaDireccion: value("addressLine"),
            codigoPostal: nullable("addressPostalCode"),
            esPrincipal: checked("addressMain")
        } });
        form.reset();
        state.direcciones = await api("/api/clientes/me/direcciones");
        renderAddresses();
        showMessage("Direccion guardada.");
    } catch (error) { showError(error); }
    finally { setBusy(form, false); }
});

document.getElementById("preferencesForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    setBusy(form, true);
    try {
        ensureClient();
        await api("/api/clientes/me/preferencias", { method: "PUT", body: {
            idiomaPreferido: value("preferredLanguage"),
            monedaPreferidaId: asIntOrNull("preferredCurrency"),
            recibePromociones: checked("receivePromotions"),
            requiereAccesibilidad: checked("requiresAccessibility"),
            observaciones: nullable("preferenceNotes")
        } });
        showMessage("Preferencias guardadas.");
    } catch (error) { showError(error); }
    finally { setBusy(form, false); }
});

document.getElementById("passwordForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    setBusy(form, true);
    try {
        await api("/api/auth/change-password", { method: "POST", body: {
            passwordActual: document.getElementById("currentPassword").value,
            passwordNueva: document.getElementById("newPassword").value
        } });
        form.reset();
        broadcastAuthChange();
        location.replace("/login.html");
    } catch (error) { showError(error); }
    finally { setBusy(form, false); }
});
