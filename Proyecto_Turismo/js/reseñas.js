document.addEventListener("DOMContentLoaded", () => {
    const btnAñadir = document.getElementById("btn-añadir");
    const btnCargar = document.getElementById("btn-cargar");
    const selectDestinos = document.getElementById("filter-destinos");
    const contenedorReseñas = document.getElementById("contenedor-reseñas");

    if (btnAñadir) {
        btnAñadir.addEventListener("click", () => {
            Swal.fire({
                title: 'Cuéntanos tu experiencia',
                html: `
                    <!-- Selector de Destino -->
                    <select id="swal-destino" class="swal2-input" style="width: 85%; max-width: 100%; font-size: 15px;">
                        <option value="" disabled selected>Selecciona el destino que visitaste</option>
                        <option value="Machu Picchu">Machu Picchu</option>
                        <option value="Montaña de los 7 colores">Montaña de los 7 colores</option>
                        <option value="Cañón del Colca">Cañón del Colca</option>
                        <option value="Lago Titicaca">Lago Titicaca</option>
                        <option value="Huaraz">Huaraz</option>
                        <option value="Iquitos">Iquitos</option>
                        <option value="Islas Ballestas">Islas Ballestas</option>
                        <option value="Máncora">Máncora</option>
                    </select>
                    
                    <!-- Estrellas Interactivas WayPath -->
                    <div id="swal-star-rating" style="color: #f97316; font-size: 32px; cursor: pointer; margin: 15px 0;">
                        <i class="fa-solid fa-star" data-value="1"></i>
                        <i class="fa-solid fa-star" data-value="2"></i>
                        <i class="fa-solid fa-star" data-value="3"></i>
                        <i class="fa-solid fa-star" data-value="4"></i>
                        <i class="fa-solid fa-star" data-value="5"></i>
                    </div>
                    <!-- Guardará el número de estrellas en secreto -->
                    <input type="hidden" id="swal-estrellas-hidden" value="5">

                    <!-- Caja de texto para la reseña -->
                    <textarea id="swal-comentario" class="swal2-textarea" placeholder="Escribe aquí los detalles de tu viaje..." style="width: 85%; max-width: 100%; font-size: 15px;"></textarea>
                `,
                focusConfirm: false,
                showCancelButton: true,
                confirmButtonText: 'Publicar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#f97316',
                cancelButtonColor: '#64748b',
                didOpen: () => {
                    const stars = document.querySelectorAll('#swal-star-rating i');
                    const hiddenInput = document.getElementById('swal-estrellas-hidden');
                    
                    stars.forEach(star => {
                        star.addEventListener('click', function() {
                            const rating = this.getAttribute('data-value');
                            hiddenInput.value = rating; 
                            
                            stars.forEach(s => {
                                if (s.getAttribute('data-value') <= rating) {
                                    s.classList.remove('fa-regular');
                                    s.classList.add('fa-solid');
                                } else {
                                    s.classList.remove('fa-solid');
                                    s.classList.add('fa-regular');
                                }
                            });
                        });
                    });
                },
                preConfirm: () => {
                    const destino = document.getElementById('swal-destino').value;
                    const estrellas = parseInt(document.getElementById('swal-estrellas-hidden').value);
                    const comentario = document.getElementById('swal-comentario').value;
                    
                    if (!destino || !comentario.trim()) {
                        Swal.showValidationMessage('Por favor selecciona un destino y escribe tu reseña');
                        return false;
                    }
                    return { destino, estrellas, comentario };
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    const datos = result.value;
                    
                    const imagenes = {
                        "Machu Picchu": "Img/machupichu.jpg",
                        "Montaña de los 7 colores": "Img/MONTAÑADELOS7COLORES.webp",
                        "Cañón del Colca": "Img/CAÑONDELCOLCA.webp",
                        "Lago Titicaca": "Img/LAGOTITICACA.webp",
                        "Huaraz": "Img/huaraz.jpg",
                        "Iquitos": "Img/iquitos.jpg",
                        "Islas Ballestas": "Img/ISLASBALLESTAS.webp",
                        "Máncora": "Img/MANCORA.jpg"
                    };
                    const rutaImagen = imagenes[datos.destino] || "Img/machupichu.jpg";

                    let estrellasHTML = "";
                    for(let i = 0; i < datos.estrellas; i++) {
                        estrellasHTML += '<i class="fa-solid fa-star"></i>';
                    }
                    for(let i = datos.estrellas; i < 5; i++) {
                        estrellasHTML += '<i class="fa-regular fa-star"></i>';
                    }

                    const nuevaTarjetaHTML = `
                        <div class="review-card">
                            <img src="${rutaImagen}" class="dest-img" alt="${datos.destino}">
                            <div class="review-body">
                                <div class="user-info">
                                    <i class="fa-solid fa-circle-user avatar-placeholder" style="color: var(--primary-orange);"></i>
                                    <div>
                                        <h4>Nuevo Viajero</h4>
                                        <span class="user-loc">📍 Viajero Frecuente</span>
                                    </div>
                                </div>
                                <p class="review-text">${datos.comentario}</p>
                            </div>
                            <div class="card-stars">
                                ${estrellasHTML}
                            </div>
                        </div>
                    `;

                    contenedorReseñas.insertAdjacentHTML('afterbegin', nuevaTarjetaHTML);

                    Swal.fire({
                        title: '¡Publicado!',
                        text: 'Tu reseña se ha agregado exitosamente.',
                        icon: 'success',
                        confirmButtonColor: '#002244'
                    });
                }
            });
        });
    }

    if (btnCargar) {
        btnCargar.addEventListener("click", () => {
            
            const reseñasExtra = `
                <!-- Reseña Extra 1 -->
                <div class="review-card">
                    <img src="Img/ISLASBALLESTAS.webp" class="dest-img" alt="Islas Ballestas">
                    <div class="review-body">
                        <div class="user-info">
                            <i class="fa-solid fa-circle-user avatar-placeholder"></i>
                            <div>
                                <h4>Jorge Mendoza</h4>
                                <span class="user-loc">📍 Ica, Perú</span>
                            </div>
                        </div>
                        <p class="review-text">Excelente paseo en bote por las Islas Ballestas. Vimos muchos lobos marinos y pingüinos. El guía explicó todo muy detalladamente.</p>
                    </div>
                    <div class="card-stars">
                        <i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i>
                    </div>
                </div>

                <!-- Reseña Extra 2 -->
                <div class="review-card">
                    <img src="Img/MANCORA.jpg" class="dest-img" alt="Máncora">
                    <div class="review-body">
                        <div class="user-info">
                            <i class="fa-solid fa-circle-user avatar-placeholder"></i>
                            <div>
                                <h4>Valeria Ríos</h4>
                                <span class="user-loc">📍 Trujillo, Perú</span>
                            </div>
                        </div>
                        <p class="review-text">Las mejores playas del norte. El hotel que nos recomendaron en WayPath tenía una vista espectacular frente al mar. Súper relajante.</p>
                    </div>
                    <div class="card-stars">
                        <i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-regular fa-star"></i>
                    </div>
                </div>
            `;

            contenedorReseñas.insertAdjacentHTML('beforeend', reseñasExtra);
            btnCargar.style.display = 'none';
        });
    }

    if (selectDestinos) {
        selectDestinos.addEventListener("change", (e) => {
            const opcion = e.target.value.toLowerCase();
            const tarjetas = document.querySelectorAll(".review-card"); 
            
            tarjetas.forEach(tarjeta => {
                const destinoImgAlt = tarjeta.querySelector(".dest-img").alt.toLowerCase();
                if (opcion === "todos los destinos" || opcion === "" || destinoImgAlt.includes(opcion)) {
                    tarjeta.style.display = "flex";
                } else {
                    tarjeta.style.display = "none";
                }
            });
        });
    }
});