(function () {
    const storage = {
        theme: "wp_theme",
        fontScale: "wp_font_scale",
        position: "waypath.accessibility.position",
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

    function ensureUtilityBar() {
        const accessibilityWidget = document.querySelector(".accessibility-widget");
        const languageWidget = document.querySelector(".traductor-widget");
        if (!accessibilityWidget && !languageWidget) return;

        let utilityBar = document.querySelector(".wp-utility-bar");
        if (!utilityBar) {
            utilityBar = document.createElement("div");
            utilityBar.className = "wp-utility-bar";
            utilityBar.setAttribute("role", "region");
            utilityBar.setAttribute("aria-label", "Preferencias de visualizacion");
            const firstWidget = languageWidget || accessibilityWidget;
            firstWidget.parentNode.insertBefore(utilityBar, firstWidget);
        }

        if (languageWidget && languageWidget.parentNode !== utilityBar) utilityBar.append(languageWidget);
        if (accessibilityWidget && accessibilityWidget.parentNode !== utilityBar) utilityBar.append(accessibilityWidget);
        utilityBar.classList.add("wp-preferences-panel");
        if (!utilityBar.querySelector(".wp-toolbar-handle")) {
            const handle = document.createElement("button");
            handle.type = "button";
            handle.className = "wp-toolbar-handle";
            handle.setAttribute("aria-label", "Mover panel de accesibilidad");
            handle.title = "Arrastrar. Doble clic para restablecer la posición";
            handle.innerHTML = '<i class="fa-solid fa-grip-vertical" aria-hidden="true"></i>';
            utilityBar.prepend(handle);
        }
        enableDragging(utilityBar);
    }

    function enableDragging(bar) {
        const margin = 8;
        const handle = bar.querySelector(".wp-toolbar-handle");
        let drag = null;
        const clamp = (x, y) => ({
            x: Math.min(Math.max(margin, x), Math.max(margin, window.innerWidth - bar.offsetWidth - margin)),
            y: Math.min(Math.max(margin, y), Math.max(margin, window.innerHeight - bar.offsetHeight - margin))
        });
        const place = (x, y, save = false) => {
            const point = clamp(x, y);
            bar.style.left = `${point.x}px`;
            bar.style.top = `${point.y}px`;
            bar.style.right = "auto";
            if (save) localStorage.setItem(storage.position, JSON.stringify(point));
        };
        try {
            const saved = JSON.parse(localStorage.getItem(storage.position));
            if (Number.isFinite(saved?.x) && Number.isFinite(saved?.y)) requestAnimationFrame(() => place(saved.x, saved.y));
        } catch { localStorage.removeItem(storage.position); }
        handle?.addEventListener("pointerdown", event => {
            if (event.button !== 0) return;
            const rect = bar.getBoundingClientRect();
            drag = { dx: event.clientX - rect.left, dy: event.clientY - rect.top };
            handle.setPointerCapture(event.pointerId);
            bar.classList.add("is-dragging");
            event.preventDefault();
        });
        handle?.addEventListener("pointermove", event => {
            if (!drag) return;
            place(event.clientX - drag.dx, event.clientY - drag.dy);
        });
        const finish = event => {
            if (!drag) return;
            drag = null;
            bar.classList.remove("is-dragging");
            const rect = bar.getBoundingClientRect();
            place(rect.left, rect.top, true);
            if (handle.hasPointerCapture(event.pointerId)) handle.releasePointerCapture(event.pointerId);
        };
        handle?.addEventListener("pointerup", finish);
        handle?.addEventListener("pointercancel", finish);
        handle?.addEventListener("dblclick", () => {
            localStorage.removeItem(storage.position);
            bar.style.left = "";
            bar.style.top = "";
            bar.style.right = "";
        });
        window.addEventListener("resize", () => {
            const rect = bar.getBoundingClientRect();
            place(rect.left, rect.top, Boolean(localStorage.getItem(storage.position)));
        });
    }

    function init() {
        const theme = readTheme();
        const scale = readFontScale();
        applyTheme(theme);
        applyFontScale(scale);
        ensureUtilityBar();
        ensureAccessibilityControls();
        setButtonState(theme, scale);
        window.addEventListener("waypath:languagechange", () => setButtonState(root.dataset.theme || "light", root.dataset.fontScale || "normal"));

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
