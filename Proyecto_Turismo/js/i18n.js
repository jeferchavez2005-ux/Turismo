const supportedLanguages = ["es", "en", "pt", "fr"];
const languageNames = {
    es: "Espanol",
    en: "English",
    pt: "Portugues",
    fr: "Francais"
};
const storageKey = "wp_language";
let dictionary = {};
let currentLanguage = "es";

function normalizeLanguage(value) {
    const lang = String(value || "").slice(0, 2).toLowerCase();
    return supportedLanguages.includes(lang) ? lang : "es";
}

async function loadDictionary(language) {
    const response = await fetch(new URL(`../locales/${language}.json`, import.meta.url));
    if (!response.ok) throw new Error(`No se pudo cargar el idioma ${language}.`);
    return response.json();
}

function translate(key) {
    return dictionary[key] || key;
}

function applyTranslations(root = document) {
    root.querySelectorAll("[data-i18n]").forEach(element => {
        element.textContent = translate(element.dataset.i18n);
    });

    root.querySelectorAll("[data-i18n-title]").forEach(element => {
        element.title = translate(element.dataset.i18nTitle);
    });

    root.querySelectorAll("[data-i18n-aria-label]").forEach(element => {
        element.setAttribute("aria-label", translate(element.dataset.i18nAriaLabel));
    });

    root.querySelectorAll("[data-i18n-placeholder]").forEach(element => {
        element.setAttribute("placeholder", translate(element.dataset.i18nPlaceholder));
    });
}

function ensureLanguageSwitcher() {
    const host = document.querySelector(".traductor-widget");
    if (!host) return;

    const existingSelect = document.getElementById("wpLanguageSelect");
    const existingLabel = host.querySelector(".wp-language-label");
    if (existingSelect) {
        existingSelect.value = currentLanguage;
        existingSelect.setAttribute("aria-label", translate("a11y.language"));
        if (existingLabel) existingLabel.textContent = translate("a11y.language");
        return;
    }

    host.innerHTML = "";
    const label = document.createElement("label");
    label.className = "wp-language-label";
    label.setAttribute("for", "wpLanguageSelect");
    label.textContent = translate("a11y.language");

    const select = document.createElement("select");
    select.id = "wpLanguageSelect";
    select.className = "wp-language-select";
    select.setAttribute("aria-label", translate("a11y.language"));

    supportedLanguages.forEach(language => {
        const option = document.createElement("option");
        option.value = language;
        option.textContent = languageNames[language];
        option.selected = language === currentLanguage;
        select.appendChild(option);
    });

    select.addEventListener("change", () => setLanguage(select.value));
    host.append(label, select);
}

async function setLanguage(language) {
    currentLanguage = normalizeLanguage(language);
    dictionary = await loadDictionary(currentLanguage);
    localStorage.setItem(storageKey, currentLanguage);
    document.documentElement.lang = currentLanguage;
    ensureLanguageSwitcher();
    const select = document.getElementById("wpLanguageSelect");
    if (select) select.value = currentLanguage;
    applyTranslations();
    window.dispatchEvent(new CustomEvent("waypath:languagechange", {
        detail: { language: currentLanguage, t: translate }
    }));
}

async function initI18n() {
    const saved = localStorage.getItem(storageKey);
    const browserLanguage = navigator.language || "es";
    await setLanguage(saved || browserLanguage);
}

window.WayPathI18n = {
    get language() { return currentLanguage; },
    t: translate,
    setLanguage,
    applyTranslations
};

initI18n().catch(() => {
    dictionary = {};
    currentLanguage = "es";
    document.documentElement.lang = "es";
    ensureLanguageSwitcher();
});
