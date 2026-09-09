document.getElementById('loginForm').addEventListener('submit', function(e) {
    e.preventDefault(); 

    const emailIngresado = document.getElementById('logCorreo').value;
    const passIngresado = document.getElementById('logPassword').value;

    const correoRegistrado = localStorage.getItem('db_correo');
    const passRegistrado = localStorage.getItem('db_password');
    const nombreRegistrado = localStorage.getItem('db_nombre');

    if (correoRegistrado && passRegistrado) {
        if (emailIngresado === correoRegistrado && passIngresado === passRegistrado) {
            
            localStorage.setItem('waypath_usuario', nombreRegistrado);
            
            Swal.fire({
                title: `¡Bienvenido, ${nombreRegistrado.split(' ')[0]}!`,
                text: 'Ingresando a WayPath...',
                icon: 'success',
                showConfirmButton: false,
                timer: 1500
            }).then(() => {
                window.location.href = 'servicios.html'; 
            });
            
        } else {
            Swal.fire({
                title: 'Datos incorrectos',
                text: 'El correo o la contraseña no coinciden.',
                icon: 'error',
                confirmButtonColor: '#0b2a4a'
            });
        }
    } else {
        Swal.fire({
            title: 'No tienes cuenta',
            text: 'Por favor, regístrate primero en WayPath.',
            icon: 'warning',
            confirmButtonColor: '#0b2a4a'
        });
    }
});