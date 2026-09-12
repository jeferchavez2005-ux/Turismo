import { api, ApiError } from "../js/api.js";

const charts = {};

document.addEventListener("DOMContentLoaded", loadDashboard);

async function loadDashboard() {
    try {
        const [summary, commercial, reservations, payments, sales, operation] = await Promise.all([
            api("/api/admin/dashboard/resumen"),
            api("/api/admin/dashboard/metricas-comerciales"),
            api("/api/admin/dashboard/reservas"),
            api("/api/admin/dashboard/pagos"),
            api("/api/admin/dashboard/ventas"),
            api("/api/admin/dashboard/operacion")
        ]);
        renderSummary(summary);
        renderCharts(commercial, reservations, payments, sales, operation);
        status("Métricas actualizadas desde WayPathDB.");
    } catch (error) {
        const message = error instanceof ApiError && error.status === 401
            ? "Inicia sesión como administrador o empleado autorizado para ver este dashboard."
            : error instanceof ApiError && error.status === 403
                ? "Tu usuario no tiene permiso REPORTES_LEER."
                : error.message || "No se pudieron cargar las métricas administrativas.";
        status(message);
        renderSummary({});
        renderCharts(emptyCommercial(), { porEstado: [] }, { porMetodo: [], porEstado: [] }, emptySales(), { cuposPorSalida: [] });
    }
}

function renderSummary(data) {
    text("kpiUsuarios", number(data.usuariosRegistrados));
    text("kpiClientes", number(data.clientesActivos));
    text("kpiReservas", number(data.reservasTotales));
    text("kpiVentas", money(data.totalVendido));
    text("kpiCompras", number(data.comprasGeneradas));
    text("kpiCupos", number(data.cuposDisponibles));
}

function renderCharts(commercial, reservations, payments, sales, operation) {
    bar("graficoConversion", ["Usuarios", "Clientes", "Reservas", "Compras"], [
        value(document.getElementById("kpiUsuarios")?.textContent),
        value(document.getElementById("kpiClientes")?.textContent),
        sum(reservations.porEstado),
        sum(sales.paquetesMasVendidos)
    ], ["#3498db", "#f1c40f", "#f97316", "#2ecc71"]);

    line("graficoIngresos", labels(sales.ingresosMensuales), values(sales.ingresosMensuales));
    doughnut("graficoDestinos", labels(commercial.destinosMasVendidos), values(commercial.destinosMasVendidos),
        ["#2ecc71", "#e67e22", "#9b59b6", "#2563eb", "#dc2626", "#14b8a6"]);
    pie("graficoPagos", labels(payments.porMetodo), values(payments.porMetodo),
        ["#34495e", "#8e44ad", "#7f8c8d", "#f97316", "#15803d"]);

    void operation;
}

function bar(id, chartLabels, data, colors) {
    createChart(id, {
        type: "bar",
        data: { labels: fallbackLabels(chartLabels), datasets: [{ label: "Cantidad", data: fallbackValues(data), backgroundColor: colors }] },
        options: chartOptions()
    });
}

function line(id, chartLabels, data) {
    createChart(id, {
        type: "line",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{
                label: "Ingresos (S/)",
                data: fallbackValues(data),
                borderColor: "#e74c3c",
                backgroundColor: "rgba(231, 76, 60, 0.16)",
                fill: true,
                tension: 0.3
            }]
        },
        options: chartOptions()
    });
}

function doughnut(id, chartLabels, data, colors) {
    createChart(id, {
        type: "doughnut",
        data: { labels: fallbackLabels(chartLabels), datasets: [{ data: fallbackValues(data), backgroundColor: colors }] },
        options: chartOptions()
    });
}

function pie(id, chartLabels, data, colors) {
    createChart(id, {
        type: "pie",
        data: { labels: fallbackLabels(chartLabels), datasets: [{ data: fallbackValues(data), backgroundColor: colors }] },
        options: chartOptions()
    });
}

function createChart(id, config) {
    const canvas = document.getElementById(id);
    if (!canvas) return;
    charts[id]?.destroy();
    charts[id] = new Chart(canvas.getContext("2d"), config);
}

function chartOptions() {
    return {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { labels: { boxWidth: 14 } } },
        scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
    };
}

function labels(rows) {
    return (rows || []).map(x => x.etiqueta);
}

function values(rows) {
    return (rows || []).map(x => Number(x.valor || 0));
}

function fallbackLabels(rows) {
    return rows?.length ? rows : ["Sin datos"];
}

function fallbackValues(rows) {
    return rows?.length && rows.some(x => Number(x) > 0) ? rows : [0];
}

function sum(rows) {
    return values(rows).reduce((acc, item) => acc + item, 0);
}

function value(textValue) {
    return Number(String(textValue || "0").replace(/[^\d.-]/g, "")) || 0;
}

function number(value) {
    return Number(value || 0).toLocaleString("es-PE");
}

function money(value) {
    return Number(value || 0).toLocaleString("es-PE", { style: "currency", currency: "PEN" });
}

function text(id, value) {
    const element = document.getElementById(id);
    if (element) element.textContent = value;
}

function status(message) {
    text("dashboardStatus", message);
}

function emptyCommercial() {
    return { ingresosMensuales: [], destinosMasVendidos: [], paquetesMasVendidos: [], metodosPago: [] };
}

function emptySales() {
    return { ingresosMensuales: [], destinosMasVendidos: [], paquetesMasVendidos: [] };
}
