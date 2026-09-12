export function icon(name) {
    return `<i class="fa-solid ${name}" aria-hidden="true"></i>`;
}

export function alert(message, type = "info") {
    return `<div class="wp-alert wp-alert-${type}" role="status">${message}</div>`;
}

export function loader(label = "Cargando...") {
    return `<div class="wp-loading" role="status" aria-live="polite"><span class="wp-loader"></span><span>${label}</span></div>`;
}

export function emptyState(title, message = "") {
    return `
        <div class="wp-empty-state">
            ${icon("fa-circle-info")}
            <strong>${title}</strong>
            ${message ? `<p>${message}</p>` : ""}
        </div>`;
}

export function statusBadge(text, type = "muted") {
    return `<span class="wp-status wp-status-${type}">${text}</span>`;
}

export function formatCurrency(value, currency = "PEN") {
    return new Intl.NumberFormat("es-PE", {
        style: "currency",
        currency
    }).format(Number(value || 0));
}

export function formatDate(value) {
    if (!value) return "";
    return new Intl.DateTimeFormat("es-PE", {
        year: "numeric",
        month: "short",
        day: "2-digit"
    }).format(new Date(value));
}
