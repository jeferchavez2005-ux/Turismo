import { api, appUrl } from "./api.js";
async function renderSession() {
    let user = null;
    try { user = await api("/api/auth/me"); } catch { /* Never trust demo storage for authentication. */ }
    document.querySelectorAll(".login-btn").forEach(button => {
        const icon = document.createElement("i");
        icon.className = user ? "fa-solid fa-user" : "fa-solid fa-right-to-bracket";
        icon.setAttribute("aria-hidden", "true");
        button.replaceChildren(icon, document.createTextNode(user ? " Mi cuenta" : " Iniciar sesion"));
        button.href = user ? appUrl("cuenta.html") : appUrl("login.html");
        button.title = user?.nombre || (user ? user.email : "Iniciar sesion");
    });
}
renderSession();
document.addEventListener("visibilitychange", () => { if (!document.hidden) renderSession(); });
if ("BroadcastChannel" in window) {
    const channel = new BroadcastChannel("waypath-auth");
    channel.addEventListener("message", renderSession);
}
