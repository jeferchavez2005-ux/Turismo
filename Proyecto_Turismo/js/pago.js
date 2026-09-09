document.addEventListener("DOMContentLoaded", () => {
    const nombreDestino = localStorage.getItem('destinoSeleccionado') || "Destino Sorpresa";
    const rutaImagen = localStorage.getItem('imgSeleccionada') || "Img/machupichu.jpg";
    const totalPagar = localStorage.getItem('wp_total_pagar') || "S/. 0.00";
    const detallePasajeros = localStorage.getItem('wp_detalle_pasajeros') || "1x Adultos (S/. 0 c/u)";

    if(document.getElementById("pago-destino")) document.getElementById("pago-destino").textContent = nombreDestino;
    if(document.getElementById("pago-img")) {
        document.getElementById("pago-img").src = rutaImagen;
        document.getElementById("pago-img").alt = nombreDestino;
    }
    if(document.getElementById("pago-pasajeros")) document.getElementById("pago-pasajeros").textContent = detallePasajeros;
    if(document.getElementById("pago-subtotal")) document.getElementById("pago-subtotal").textContent = totalPagar;
    if(document.getElementById("pago-gran-total")) document.getElementById("pago-gran-total").textContent = totalPagar;

    const tarjetasMetodos = document.querySelectorAll(".metodo-card");
    const formTarjeta = document.getElementById("form-tarjeta");
    const formYape = document.getElementById("form-yape");

    tarjetasMetodos.forEach(tarjeta => {
        tarjeta.addEventListener("click", function() {
            tarjetasMetodos.forEach(t => t.classList.remove("activo"));
            this.classList.add("activo");

            if (this.getAttribute("data-metodo") === "yape") {
                if(formTarjeta) formTarjeta.style.display = "none";
                if(formYape) formYape.style.display = "block";
            } else {
                if(formYape) formYape.style.display = "none";
                if(formTarjeta) formTarjeta.style.display = "flex";
            }
        });
    });

    function procesarPago(e) {
        if (e) e.preventDefault();

        Swal.fire({
            title: 'Procesando pago...',
            html: 'Conectando con la pasarela de WayPath de forma segura<br><br><i class="fa-solid fa-circle-notch fa-spin fa-3x" style="color: var(--primary-orange);"></i>',
            showConfirmButton: false,
            allowOutsideClick: false,
            timer: 2000
        }).then(() => {
            Swal.fire({
                title: '¡Pago Exitoso!',
                text: 'Tu reserva ha sido procesada correctamente.',
                icon: 'success',
                confirmButtonColor: '#002244',
                confirmButtonText: 'Ver mis compras'
            }).then((result) => {
                if (result.isConfirmed) {
                    localStorage.setItem('wp_pago_exitoso', 'true');
                    window.location.href = 'compras.html';
                }
            });
        });
    }

    if(formTarjeta) formTarjeta.addEventListener("submit", procesarPago);
    if(document.getElementById("btn-yape-pagado")) document.getElementById("btn-yape-pagado").addEventListener("click", procesarPago);
});