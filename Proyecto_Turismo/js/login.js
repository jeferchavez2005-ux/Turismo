import { api, broadcastAuthChange } from "./api.js";
const form = document.getElementById("loginForm");
const message = document.getElementById("authMessage");
if (new URLSearchParams(location.search).get("registered") === "1")
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
        location.assign("/index.html");
    } catch (error) { message.textContent = error.message; }
    finally { button.disabled = false; }
});
document.querySelector(".forgot a").addEventListener("click", event => {
    event.preventDefault();
    message.textContent = "La recuperacion de cuentas por correo aun no esta disponible.";
});
