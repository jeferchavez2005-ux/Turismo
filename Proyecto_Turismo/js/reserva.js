document.addEventListener("DOMContentLoaded", () => {
    const nombreDestino = localStorage.getItem('destinoSeleccionado') || "Destino Turístico";
    const precioBase = parseFloat(localStorage.getItem('precioSeleccionado')) || 180;
    const rutaImagen = localStorage.getItem('imgSeleccionada') || "Img/MONTAÑADELOS7COLORES.webp";
    
    const imgBanner = document.getElementById("destino-img");
    const inputDestinoFijo = document.getElementById("destino-nombre-input");
    
    if (imgBanner) {
        imgBanner.src = rutaImagen;
        imgBanner.alt = nombreDestino;
    }
    if (inputDestinoFijo) {
        inputDestinoFijo.value = nombreDestino;
    }

    const selectViajeros = document.getElementById("viajeros");
    const detalleCantPasajeros = document.getElementById("cant-pasajeros");
    const detalleSubtotal = document.getElementById("subtotal-pasajeros");
    const detalleExtras = document.getElementById("subtotal-extras");
    const totalPagar = document.getElementById("total-pagar");
    const btnConfirmar = document.getElementById("btn-confirmar");
    const tarjetasExtras = document.querySelectorAll(".extra-card");

    function actualizarResumen() {
        const cantidad = parseInt(selectViajeros.value) || 1;
        const subtotalPasajeros = cantidad * precioBase;

        let totalExtras = 0;
        document.querySelectorAll('.extra-card.seleccionado').forEach(tarjeta => {
            totalExtras += parseFloat(tarjeta.getAttribute('data-precio'));
        });

        const granTotal = subtotalPasajeros + totalExtras;
        
        if(detalleCantPasajeros) detalleCantPasajeros.textContent = `${cantidad}x Adultos (S/. ${precioBase} c/u)`;
        if(detalleSubtotal) detalleSubtotal.textContent = `S/. ${subtotalPasajeros.toFixed(2)}`;
        if(detalleExtras) detalleExtras.textContent = `+ S/. ${totalExtras.toFixed(2)}`;
        if(totalPagar) totalPagar.textContent = `S/. ${granTotal.toFixed(2)}`;
    }

    if (selectViajeros) {
        selectViajeros.addEventListener("change", actualizarResumen);
    }

    tarjetasExtras.forEach(tarjeta => {
        tarjeta.addEventListener('click', function() {
            this.classList.toggle('seleccionado');
            actualizarResumen();
        });
    });

    if (btnConfirmar) {
        btnConfirmar.addEventListener("click", (e) => {
            e.preventDefault(); 
            const granTotal = document.getElementById("total-pagar").textContent;
            const cantPasajerosText = document.getElementById("cant-pasajeros").textContent;
            
            localStorage.setItem("wp_total_pagar", granTotal);
            localStorage.setItem("wp_detalle_pasajeros", cantPasajerosText);
            
            window.location.href = "pago.html";
        });
    }

    actualizarResumen();
});