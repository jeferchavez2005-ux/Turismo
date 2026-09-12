export class ApiError extends Error {
    constructor(message, status) { super(message); this.status = status; }
}
export function removeDemoCredentials() {
    try {
        ["waypath_usuario", "db_password", "db_correo", "db_nombre"].forEach(key => localStorage.removeItem(key));
    } catch { /* Storage can be disabled. */ }
}
async function request(path, options = {}) {
    let response;
    try {
        response = await fetch(path, {
            ...options, credentials: "same-origin", signal: AbortSignal.timeout(15000),
            headers: { Accept: "application/json", ...options.headers }
        });
    } catch { throw new ApiError("No se pudo conectar con WayPath. Intenta nuevamente.", 0); }
    if (response.status === 204) return null;
    const body = (response.headers.get("content-type") || "").includes("json") ? await response.json() : null;
    if (!response.ok) {
        const message = response.status === 429 ? "Demasiados intentos. Espera un minuto."
            : response.status === 401 ? (path === "/api/auth/login" ? "Correo o clave incorrectos." : "Tu sesión expiró. Inicia sesión nuevamente.")
            : response.status === 403 ? "No tienes permiso para realizar esta operacion."
            : body?.title || "No se pudo completar la solicitud. Revisa los datos e intenta nuevamente.";
        throw new ApiError(message, response.status);
    }
    if (!body) throw new ApiError("La API de WayPath no esta disponible en este servidor.", 0);
    return body;
}
export async function api(path, { method = "GET", body } = {}) {
    const headers = {};
    if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
        const csrf = await request("/api/auth/csrf");
        headers["X-CSRF-TOKEN"] = csrf.token;
    }
    if (body !== undefined) headers["Content-Type"] = "application/json";
    return request(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
}
export function broadcastAuthChange() {
    if ("BroadcastChannel" in window) {
        const channel = new BroadcastChannel("waypath-auth");
        channel.postMessage("changed");
        channel.close();
    }
}
removeDemoCredentials();
