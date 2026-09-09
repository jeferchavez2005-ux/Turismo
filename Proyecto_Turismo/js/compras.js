document.addEventListener("DOMContentLoaded", () => {
    
    const pagoExitoso = localStorage.getItem('wp_pago_exitoso');
    
    if (pagoExitoso === 'true') {
        const nombreDestino = localStorage.getItem('destinoSeleccionado') || "Destino Turístico";
        const rutaImagen = localStorage.getItem('imgSeleccionada') || "Img/machupichu.jpg";
        const totalPagar = localStorage.getItem('wp_total_pagar') || "S/. 0.00";
        const detallePasajeros = localStorage.getItem('wp_detalle_pasajeros') || "1x Adultos";

        const contenedorCompras = document.getElementById("contenedor-compras");
        const codigoAleatorio = Math.floor(Math.random() * 9000) + 1000;
        
        if (contenedorCompras) {
            const nuevaCompraHTML = `
                <article class="purchase-wrapper" data-status="proxima">
                    <div class="purchase-card" style="border-left: 4px solid #f97316;">
                        <img src="${rutaImagen}" alt="${nombreDestino}">
                        <div class="purchase-info">
                            <h3>Aventura en ${nombreDestino}</h3>
                            <div class="details">
                                <i class="fa-solid fa-location-dot"></i> Perú &nbsp;|&nbsp; 
                                <i class="fa-regular fa-calendar"></i> Próximamente &nbsp;|&nbsp; 
                                <i class="fa-regular fa-user"></i> ${detallePasajeros.split('x')[0]} viajero(s)
                            </div>
                            <div class="code">Código de reserva: WP-2026-${codigoAleatorio}</div>
                        </div>
                        <div class="purchase-side">
                            <div class="badge badge-blue" style="background-color: #f97316; color: white;">¡Nuevo!</div>
                            <div class="price-label">Total pagado</div>
                            <div class="price">${totalPagar}</div>
                            <button class="details-btn">Ver detalles <i class="fa-solid fa-arrow-down"></i></button>
                        </div>
                    </div>
                    <div class="trip-details">
                        <h4>Itinerario y detalles de la reserva:</h4>
                        <ul>
                            <li><strong>Hotel:</strong> Asignado según paquete estándar (4 noches)</li>
                            <li><strong>Guía:</strong> Servicio local en español incluido</li>
                            <li><strong>Estado del Ticket:</strong> Confirmado y listo para descargar</li>
                        </ul>
                        <div class="details-actions">
                            <button class="btn-outline">Descargar Tickets</button>
                            <button class="btn-outline">Modificar Reserva</button>
                        </div>
                    </div>
                </article>
            `;
            contenedorCompras.insertAdjacentHTML('afterbegin', nuevaCompraHTML);
        }
        
        localStorage.removeItem('wp_pago_exitoso');
    }

    document.addEventListener('click', function(e) {
        const btn = e.target.closest('.details-btn');
        if (!btn) return; 
        
        e.preventDefault();
        
        const wrapper = btn.closest('.purchase-wrapper');
        const panelDetalles = wrapper.querySelector('.trip-details');
        
        if (panelDetalles) {
            panelDetalles.style.display = 'block';
            
            const estaAbierto = panelDetalles.style.maxHeight && panelDetalles.style.maxHeight !== '0px';
            
            if (estaAbierto) {
                panelDetalles.style.maxHeight = '0px';
                panelDetalles.style.padding = '0px 20px';
                panelDetalles.style.borderTopColor = 'transparent';
                
                btn.innerHTML = 'Ver detalles <i class="fa-solid fa-arrow-down"></i>';
            } else {
                panelDetalles.style.maxHeight = panelDetalles.scrollHeight + 60 + 'px'; 
                panelDetalles.style.padding = '20px';
                panelDetalles.style.borderTopColor = 'var(--border-color)';
                
                btn.innerHTML = 'Ocultar detalles <i class="fa-solid fa-arrow-up"></i>';
            }
        }
    });

    const tabs = document.querySelectorAll('.tab');
    tabs.forEach(tab => {
        tab.addEventListener('click', function() {
            tabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');

            const filterValue = this.getAttribute('data-filter');
            const articles = document.querySelectorAll('.purchase-wrapper');

            articles.forEach(article => {
                if (filterValue === 'all') {
                    article.style.display = 'block';
                } else {
                    if (article.getAttribute('data-status') === filterValue) {
                        article.style.display = 'block';
                    } else {
                        article.style.display = 'none';
                    }
                }
            });
        });
    });
});