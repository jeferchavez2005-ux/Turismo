(function () {
    const storage = {
        theme: "wp_theme",
        fontScale: "wp_font_scale",
        legacyContrast: "wp_alto_contraste",
        legacyLargeText: "wp_texto_grande"
    };

    const root = document.documentElement;
    const prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;

    function readTheme() {
        const saved = localStorage.getItem(storage.theme);
        if (saved === "light" || saved === "dark") return saved;
        if (localStorage.getItem(storage.legacyContrast) === "true") return "dark";
        return prefersDark ? "dark" : "light";
    }

    function readFontScale() {
        const saved = localStorage.getItem(storage.fontScale);
        if (["small", "normal", "large"].includes(saved)) return saved;
        return localStorage.getItem(storage.legacyLargeText) === "true" ? "large" : "normal";
    }

    function applyTheme(theme) {
        root.dataset.theme = theme;
        document.body.classList.toggle("high-contrast", theme === "dark");
        localStorage.setItem(storage.theme, theme);
        localStorage.setItem(storage.legacyContrast, String(theme === "dark"));
    }

    function applyFontScale(scale) {
        root.dataset.fontScale = scale;
        document.body.classList.toggle("large-text", scale === "large");
        localStorage.setItem(storage.fontScale, scale);
        localStorage.setItem(storage.legacyLargeText, String(scale === "large"));
    }

    function setButtonState(theme, scale) {
        const themeButton = document.getElementById("btn-alto-contraste");
        const decreaseButton = document.getElementById("btn-reducir-texto");
        const normalButton = document.getElementById("btn-texto-normal");
        const increaseButton = document.getElementById("btn-aumentar-texto");

        if (themeButton) {
            themeButton.setAttribute("aria-label", theme === "dark" ? "Cambiar a modo claro" : "Cambiar a modo oscuro");
            themeButton.setAttribute("aria-pressed", String(theme === "dark"));
            themeButton.title = theme === "dark" ? "Modo claro" : "Modo oscuro";
            themeButton.innerHTML = theme === "dark"
                ? '<i class="fa-solid fa-sun" aria-hidden="true"></i>'
                : '<i class="fa-solid fa-moon" aria-hidden="true"></i>';
        }

        [
            [decreaseButton, "small"],
            [normalButton, "normal"],
            [increaseButton, "large"]
        ].forEach(([button, value]) => {
            if (!button) return;
            button.setAttribute("aria-pressed", String(scale === value));
            button.classList.toggle("is-active", scale === value);
        });
    }

    function ensureAccessibilityControls() {
        const widget = document.querySelector(".accessibility-widget");
        if (!widget) return;

        const decreaseButton = document.getElementById("btn-reducir-texto");
        const increaseButton = document.getElementById("btn-aumentar-texto");

        if (decreaseButton) {
            decreaseButton.dataset.fontSize = "small";
            decreaseButton.setAttribute("aria-label", "Reducir tamano de texto");
            decreaseButton.title = "Texto pequeno";
            decreaseButton.textContent = "A-";
        }

        if (!document.getElementById("btn-texto-normal") && increaseButton) {
            const normalButton = document.createElement("button");
            normalButton.id = "btn-texto-normal";
            normalButton.type = "button";
            normalButton.dataset.fontSize = "normal";
            normalButton.setAttribute("aria-label", "Restablecer tamano de texto");
            normalButton.title = "Texto normal";
            normalButton.textContent = "A";
            widget.insertBefore(normalButton, increaseButton);
        }

        if (increaseButton) {
            increaseButton.dataset.fontSize = "large";
            increaseButton.setAttribute("aria-label", "Aumentar tamano de texto");
            increaseButton.title = "Texto grande";
            increaseButton.textContent = "A+";
        }
    }

    function init() {
        const theme = readTheme();
        const scale = readFontScale();
        applyTheme(theme);
        applyFontScale(scale);
        ensureAccessibilityControls();
        setButtonState(theme, scale);

        document.getElementById("btn-alto-contraste")?.addEventListener("click", () => {
            const nextTheme = root.dataset.theme === "dark" ? "light" : "dark";
            applyTheme(nextTheme);
            setButtonState(nextTheme, root.dataset.fontScale || "normal");
        });

        document.getElementById("btn-reducir-texto")?.addEventListener("click", () => {
            applyFontScale("small");
            setButtonState(root.dataset.theme || "light", "small");
        });

        document.getElementById("btn-texto-normal")?.addEventListener("click", () => {
            applyFontScale("normal");
            setButtonState(root.dataset.theme || "light", "normal");
        });

        document.getElementById("btn-aumentar-texto")?.addEventListener("click", () => {
            applyFontScale("large");
            setButtonState(root.dataset.theme || "light", "large");
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();

function googleTranslateElementInit() {
    if (!window.WayPathI18n && window.google?.translate) {
        new google.translate.TranslateElement({
            pageLanguage: "es",
            includedLanguages: "en,pt,fr",
            layout: google.translate.TranslateElement.InlineLayout.SIMPLE
        }, "google_translate_element");
    }
}

import("./i18n.js").catch(() => {});
