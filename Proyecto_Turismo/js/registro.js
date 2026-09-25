import { api, appPath, appUrl } from "./api.js";
const form = document.getElementById("registerForm");
const message = document.getElementById("authMessage");
const password = document.getElementById("password");
const confirmPassword = document.getElementById("confirmPassword");
const strength = document.getElementById("passwordStrength");
const params = new URLSearchParams(location.search);
const requestedReturn = safeReturnUrl(params.get("returnUrl") || localStorage.getItem("wp_auth_return_url"));

password?.addEventListener("input", renderPasswordStrength);
confirmPassword?.addEventListener("input", validatePasswordMatch);

form.addEventListener("submit", async event => {
    event.preventDefault();
    validatePasswordMatch();
    if (!isStrongPassword(password.value)) {
        message.textContent = "La contraseña debe tener mínimo 12 caracteres y al menos 4 caracteres especiales.";
        return;
    }
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
        const suffix = requestedReturn ? `&returnUrl=${encodeURIComponent(requestedReturn)}` : "";
        location.assign(`${appPath("login.html")}?registered=1${suffix}`);
    } catch (error) { message.textContent = error.message; }
    finally { button.disabled = false; }
});

function renderPasswordStrength() {
    const score = passwordScore(password.value);
    strength?.classList.toggle("strong", score === 3);
    strength?.classList.toggle("medium", score === 2);
    strength?.classList.toggle("weak", score < 2);
    const label = score === 3 ? "Contraseña segura" : score === 2 ? "Contraseña aceptable" : "Contraseña débil";
    const bar = strength?.querySelector("span");
    const text = strength?.querySelector("small");
    if (bar) bar.style.width = `${Math.max(25, score * 33)}%`;
    if (text) text.textContent = `${label}. Usa mínimo 12 caracteres y 4 especiales.`;
}

function validatePasswordMatch() {
    if (!confirmPassword) return;
    confirmPassword.setCustomValidity(password.value === confirmPassword.value ? "" : "Las contraseñas no coinciden.");
}

function passwordScore(value) {
    let score = 0;
    if (value.length >= 12) score += 1;
    if ((value.match(/[^A-Za-z0-9]/g) || []).length >= 4) score += 1;
    if (/[A-Z]/.test(value) && /[a-z]/.test(value) && /\d/.test(value)) score += 1;
    return score;
}

function isStrongPassword(value) {
    return value.length >= 12 && (value.match(/[^A-Za-z0-9]/g) || []).length >= 4;
}

function safeReturnUrl(value) {
    if (!value) return "";
    if (!value.startsWith("/") || value.startsWith("//")) return "";
    const candidate = new URL(value, location.origin);
    const appRoot = new URL(appUrl());
    return candidate.origin === location.origin && candidate.pathname.startsWith(appRoot.pathname)
        ? `${candidate.pathname}${candidate.search}${candidate.hash}`
        : "";
}
