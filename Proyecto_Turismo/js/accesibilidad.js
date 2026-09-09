document.addEventListener("DOMContentLoaded", () => {
    const body = document.body;
    const btnAltoContraste = document.getElementById("btn-alto-contraste");
    const btnAumentarTexto = document.getElementById("btn-aumentar-texto");
    const btnReducirTexto = document.getElementById("btn-reducir-texto");

    // Recuperar estados previos del almacenamiento local
    if (localStorage.getItem("wp_alto_contraste") === "true") {
        body.classList.add("high-contrast");
    }
    if (localStorage.getItem("wp_texto_grande") === "true") {
        body.classList.add("large-text");
    }

    // Alternar modo de alto contraste
    if (btnAltoContraste) {
        btnAltoContraste.addEventListener("click", () => {
            body.classList.toggle("high-contrast");
            localStorage.setItem("wp_alto_contraste", body.classList.contains("high-contrast"));
        });
    }

    // Aumentar tamaño de letra
    if (btnAumentarTexto) {
        btnAumentarTexto.addEventListener("click", () => {
            body.classList.add("large-text");
            localStorage.setItem("wp_texto_grande", "true");
        });
    }

    // Reducir tamaño de letra
    if (btnReducirTexto) {
        btnReducirTexto.addEventListener("click", () => {
            body.classList.remove("large-text");
            localStorage.setItem("wp_texto_grande", "false");
        });
    }
});

// Inicializador del componente de Google Translate
function googleTranslateElementInit() {
    new google.translate.TranslateElement({
        pageLanguage: 'es',
        includedLanguages: 'en,fr,de,it,pt',
        layout: google.translate.TranslateElement.InlineLayout.SIMPLE
    }, 'google_translate_element');
}