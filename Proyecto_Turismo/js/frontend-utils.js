export function t(key, fallback = key) {
    const translated = window.WayPathI18n?.t(key);
    return translated && translated !== key ? translated : fallback;
}

export function localDateIso(date = new Date()) {
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 10);
}

export function previousDateIso(date = new Date()) {
    const previous = new Date(date);
    previous.setDate(previous.getDate() - 1);
    return localDateIso(previous);
}

export function isValidPastDate(value, today = localDateIso()) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value || "") || value >= today) return false;
    const [year, month, day] = value.split("-").map(Number);
    const date = new Date(Date.UTC(year, month - 1, day));
    return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
}

export function safeInternalReturnUrl(value, appUrl) {
    if (!value || !value.startsWith("/") || value.startsWith("//")) return "";
    try {
        const candidate = new URL(value, location.origin);
        const root = new URL(appUrl());
        return candidate.origin === location.origin && candidate.pathname.startsWith(root.pathname)
            ? `${candidate.pathname}${candidate.search}${candidate.hash}` : "";
    } catch { return ""; }
}

export function errorMessageWithTrace(error, fallback) {
    const message = error?.message || fallback;
    return error?.traceId ? `${message} ${t("errors.trace", "Código de diagnóstico")}: ${error.traceId}` : message;
}
