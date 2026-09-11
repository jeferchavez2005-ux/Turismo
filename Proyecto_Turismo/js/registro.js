import { api } from "./api.js";
const form = document.getElementById("registerForm");
const message = document.getElementById("authMessage");
form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    const button = form.querySelector('button[type="submit"]');
    button.disabled = true;
    message.textContent = "Creando cuenta...";
    try {
        await api("/api/auth/register", { method: "POST", body: {
            nombre: document.getElementById("name").value.trim(),
            email: document.getElementById("email").value.trim(),
            password: document.getElementById("password").value
        } });
        form.reset();
        location.assign("/login.html?registered=1");
    } catch (error) { message.textContent = error.message; }
    finally { button.disabled = false; }
});
