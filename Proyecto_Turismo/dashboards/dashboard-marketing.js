import { api, ApiError } from "../js/api.js";

const charts = {};

document.addEventListener("DOMContentLoaded", loadReports);

async function loadReports() {
    try {
        const [clientes, eventos, resenas, operacion] = await Promise.all([
            api("/api/reportes/clientes"),
            api("/api/reportes/eventos/resumen"),
            api("/api/reportes/resenas"),
            api("/api/reportes/operacion")
        ]);
        chartBar("graficoCampanas", labels(eventos), values(eventos), "Eventos registrados", true);
        chartBar("graficoSocial", labels(clientes.nuevosPorMes), values(clientes.nuevosPorMes), "Clientes nuevos", false);
        chartPolar("graficoEdades", labels(resenas), values(resenas));
        chartDoughnut("graficoDispositivos", labels(operacion.salidasPorEstado), values(operacion.salidasPorEstado));
        status("Reportes cargados desde WayPathDB.");
    } catch (error) {
        const message = error instanceof ApiError && error.status === 403
            ? "Tu usuario no tiene permiso REPORTES_LEER."
            : error.message || "No se pudieron cargar los reportes.";
        status(message);
        chartBar("graficoCampanas", ["Sin datos"], [0], "Eventos registrados", true);
        chartBar("graficoSocial", ["Sin datos"], [0], "Clientes nuevos", false);
        chartPolar("graficoEdades", ["Sin datos"], [0]);
        chartDoughnut("graficoDispositivos", ["Sin datos"], [0]);
    }
}

function chartBar(id, chartLabels, data, label, horizontal) {
    createChart(id, {
        type: "bar",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ label, data: fallbackValues(data), backgroundColor: ["#f39c12", "#8e44ad", "#c0392b", "#e74c3c", "#2563eb"] }]
        },
        options: { responsive: true, maintainAspectRatio: false, indexAxis: horizontal ? "y" : "x", scales: { y: { beginAtZero: true } } }
    });
}

function chartPolar(id, chartLabels, data) {
    createChart(id, {
        type: "polarArea",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ data: fallbackValues(data), backgroundColor: ["#ff6384b3", "#36a2ebb3", "#ffce56b3", "#4bc0c0b3", "#9966ffb3"] }]
        },
        options: { responsive: true, maintainAspectRatio: false }
    });
}

function chartDoughnut(id, chartLabels, data) {
    createChart(id, {
        type: "doughnut",
        data: {
            labels: fallbackLabels(chartLabels),
            datasets: [{ data: fallbackValues(data), backgroundColor: ["#3498db", "#2ecc71", "#95a5a6", "#f97316"] }]
        },
        options: { responsive: true, maintainAspectRatio: false }
    });
}

function createChart(id, config) {
    const canvas = document.getElementById(id);
    if (!canvas) return;
    charts[id]?.destroy();
    charts[id] = new Chart(canvas.getContext("2d"), config);
}

function labels(rows) {
    return (rows || []).map(x => x.etiqueta || x.tipoEvento);
}

function values(rows) {
    return (rows || []).map(x => Number(x.valor ?? x.total ?? 0));
}

function fallbackLabels(rows) {
    return rows?.length ? rows : ["Sin datos"];
}

function fallbackValues(rows) {
    return rows?.length && rows.some(x => Number(x) > 0) ? rows : [0];
}

function status(message) {
    const element = document.getElementById("reportesStatus");
    if (element) element.textContent = message;
}
