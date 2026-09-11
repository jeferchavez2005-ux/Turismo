import { api, broadcastAuthChange } from "./api.js";
const message = document.getElementById("accountMessage");
const logout = document.getElementById("logout");
function showError(error) {
    if (error.status === 401) { location.replace("/login.html"); return; }
    message.textContent = error.message;
}
try {
    const user = await api("/api/auth/me");
    document.getElementById("accountEmail").textContent = user.email;
    document.getElementById("name").value = user.nombre || "";
    document.getElementById("accountContent").hidden = false;
    logout.disabled = false;
    message.textContent = "";
} catch (error) { showError(error); }
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
    const button = event.currentTarget.querySelector("button");
    button.disabled = true;
    try {
        await api("/api/auth/profile", { method: "PUT", body: { nombre: document.getElementById("name").value.trim() } });
        message.textContent = "Perfil actualizado.";
        broadcastAuthChange();
    } catch (error) { showError(error); }
    finally { button.disabled = false; }
});
document.getElementById("passwordForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    const button = form.querySelector("button");
    button.disabled = true;
    try {
        await api("/api/auth/change-password", { method: "POST", body: {
            passwordActual: document.getElementById("currentPassword").value,
            passwordNueva: document.getElementById("newPassword").value
        } });
        form.reset();
        broadcastAuthChange();
        location.replace("/login.html");
    } catch (error) { showError(error); }
    finally { button.disabled = false; }
});

