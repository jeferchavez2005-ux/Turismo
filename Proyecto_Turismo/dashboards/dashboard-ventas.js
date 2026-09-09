const ctxConversion = document.getElementById('graficoConversion').getContext('2d');
new Chart(ctxConversion, {
    type: 'bar',
    data: {
        labels: ['Visitas al sitio', 'Inician Reserva', 'Pagan y Compran'],
        datasets: [{
            label: 'Cantidad de Usuarios',
            data: [1500, 600, 200],
            backgroundColor: ['#3498db', '#f1c40f', '#2ecc71']
        }]
    }
});

const ctxDestinos = document.getElementById('graficoDestinos').getContext('2d');
new Chart(ctxDestinos, {
    type: 'doughnut',
    data: {
        labels: ['Machu Picchu', 'Nasca', 'Máncora'],
        datasets: [{
            data: [50, 30, 20],
            backgroundColor: ['#2ecc71', '#e67e22', '#9b59b6']
        }]
    }
});

const ctxIngresos = document.getElementById('graficoIngresos').getContext('2d');
new Chart(ctxIngresos, {
    type: 'line',
    data: {
        labels: ['Enero', 'Febrero', 'Marzo', 'Abril'],
        datasets: [{
            label: 'Ingresos (S/.)',
            data: [4500, 6800, 6200, 8900],
            borderColor: '#e74c3c',
            backgroundColor: 'rgba(231, 76, 60, 0.2)',
            fill: true,
            tension: 0.3
        }]
    }
});

const ctxPagos = document.getElementById('graficoPagos').getContext('2d');
new Chart(ctxPagos, {
    type: 'pie',
    data: {
        labels: ['Tarjeta de Crédito', 'Yape', 'Tarjeta de Débito'],
        datasets: [{
            data: [55, 35, 10],
            backgroundColor: ['#34495e', '#8e44ad', '#7f8c8d']
        }]
    }
});