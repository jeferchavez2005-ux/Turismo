document.getElementById('registroForm').addEventListener('submit', function(e) {
    e.preventDefault(); 

    const nombre = document.getElementById('regNombre').value;
    const correo = document.getElementById('regCorreo').value;
    const password = document.getElementById('regPassword').value;

    localStorage.setItem('db_nombre', nombre);
    localStorage.setItem('db_correo', correo);
    localStorage.setItem('db_password', password);

    Swal.fire({
        title: '¡Registro exitoso!',
        text: 'Tu cuenta ha sido creada. Ahora inicia sesión.',
        icon: 'success',
        confirmButtonColor: '#ff7a00',
        confirmButtonText: 'Ir a Iniciar Sesión'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = 'login.html';
        }
    });
});