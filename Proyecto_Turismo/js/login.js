import { api, broadcastAuthChange } from "./api.js";
const form = document.getElementById("loginForm");
const message = document.getElementById("authMessage");
const params = new URLSearchParams(location.search);
const requestedReturn = safeReturnUrl(params.get("returnUrl") || localStorage.getItem("wp_auth_return_url"));
const registerLink = document.getElementById("registerFromLogin");
if (registerLink && requestedReturn) registerLink.href = `registro.html?returnUrl=${encodeURIComponent(requestedReturn)}`;
if (params.get("registered") === "1")
    message.textContent = "Cuenta creada. Inicia sesion para continuar.";
form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    const button = form.querySelector('button[type="submit"]');
    button.disabled = true;
    message.textContent = "Verificando...";
    try {
        await api("/api/auth/login", { method: "POST", body: {
            email: document.getElementById("email").value.trim(),
            password: document.getElementById("password").value
        } });
        form.reset();
        broadcastAuthChange();
        const next = requestedReturn || "/index.html";
        localStorage.removeItem("wp_auth_return_url");
        location.assign(next);
    } catch (error) { message.textContent = error.message; }
    finally { button.disabled = false; }
});
document.querySelector(".forgot a").addEventListener("click", event => {
    event.preventDefault();
    message.textContent = "La recuperacion de cuentas por correo aun no esta disponible.";
});

function safeReturnUrl(value) {
    if (!value) return "";
    if (!value.startsWith("/") || value.startsWith("//")) return "";
    return value;
}
