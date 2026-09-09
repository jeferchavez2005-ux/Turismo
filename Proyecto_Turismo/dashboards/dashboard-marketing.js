const ctxCampanas = document.getElementById('graficoCampanas').getContext('2d');
new Chart(ctxCampanas, {
    type: 'bar',
    data: {
        labels: ['Verano 2026', 'Cyber Travel', 'Fiestas Patrias', 'San Valentín'],
        datasets: [{
            label: 'Retorno de Inversión (S/.)',
            data: [15000, 22000, 35000, 12000],
            backgroundColor: ['#f39c12', '#8e44ad', '#c0392b', '#e74c3c']
        }]
    },
    options: {
        indexAxis: 'y', 
    }
});

const ctxSocial = document.getElementById('graficoSocial').getContext('2d');
new Chart(ctxSocial, {
    type: 'bar',
    data: {
        labels: ['Instagram', 'TikTok', 'Facebook', 'X (Twitter)'],
        datasets: [{
            label: 'Interacciones Mensuales',
            data: [12000, 15500, 8000, 3000],
            backgroundColor: ['#e1306c', '#000000', '#1877f2', '#1da1f2']
        }]
    }
});

const ctxEdades = document.getElementById('graficoEdades').getContext('2d');
new Chart(ctxEdades, {
    type: 'polarArea',
    data: {
        labels: ['18-24 años', '25-34 años', '35-44 años', '45+ años'],
        datasets: [{
            data: [25, 40, 20, 15],
            backgroundColor: [
                'rgba(255, 99, 132, 0.7)',
                'rgba(54, 162, 235, 0.7)',
                'rgba(255, 206, 86, 0.7)',
                'rgba(75, 192, 192, 0.7)'
            ]
        }]
    }
});

const ctxDispositivos = document.getElementById('graficoDispositivos').getContext('2d');
new Chart(ctxDispositivos, {
    type: 'doughnut',
    data: {
        labels: ['Computadora', 'Celular (Móvil)', 'Laptop'],
        datasets: [{
            data: [60, 30, 10], 
            backgroundColor: ['#3498db', '#2ecc71', '#95a5a6'] 
        }]
    }
});