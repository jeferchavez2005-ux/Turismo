using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "permission:REPORTES_LEER")]
public sealed class ReportesController(ReporteService service) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken ct) => Ok(await service.Kpis(ct));

    [HttpGet("metricas-diarias")]
    public async Task<IActionResult> MetricasDiarias([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] string? nombreMetrica, [FromQuery] string? dimensionNombre,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await service.DailyMetrics(desde, hasta, nombreMetrica, dimensionNombre, page, pageSize, ct));

    [HttpPost("metricas-diarias/generar")]
    public async Task<IActionResult> GenerarMetricasDiarias([FromQuery] DateOnly? fecha, CancellationToken ct) =>
        Ok(new { actualizadas = await service.GenerateDailyMetrics(fecha ?? DateOnly.FromDateTime(DateTime.UtcNow), ct) });

    [HttpGet("comercial")]
    public async Task<IActionResult> Comercial(CancellationToken ct) => Ok(await service.Commercial(ct));

    [HttpGet("reservas")]
    public async Task<IActionResult> Reservas(CancellationToken ct) => Ok(await service.Reservations(ct));

    [HttpGet("pagos")]
    public async Task<IActionResult> Pagos(CancellationToken ct) => Ok(await service.Payments(ct));

    [HttpGet("operacion")]
    public async Task<IActionResult> Operacion(CancellationToken ct) => Ok(await service.Operation(ct));

    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes(CancellationToken ct) => Ok(await service.Clients(ct));

    [HttpGet("resenas")]
    public async Task<IActionResult> Resenas(CancellationToken ct) => Ok(await service.Reviews(ct));

    [HttpPost("eventos")]
    public async Task<IActionResult> CrearEvento(EventoAnaliticaRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateEvent(UserIdOrNull, HttpContext.TraceIdentifier,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), request, ct));

    [HttpGet("eventos")]
    public async Task<IActionResult> Eventos([FromQuery] string? tipoEvento,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await service.Events(tipoEvento, desde, hasta, page, pageSize, ct));

    [HttpGet("eventos/resumen")]
    public async Task<IActionResult> EventosResumen(CancellationToken ct) => Ok(await service.EventSummary(ct));

    [HttpGet("snapshots")]
    public async Task<IActionResult> Snapshots([FromQuery] string? codigoDashboard,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.Snapshots(codigoDashboard, page, pageSize, ct));

    [HttpGet("snapshots/{id:long}")]
    public async Task<IActionResult> Snapshot(long id, CancellationToken ct)
    {
        var snapshot = await service.Snapshot(id, ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    [HttpPost("snapshots")]
    public async Task<IActionResult> CrearSnapshot(DashboardSnapshotRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateSnapshot(UserId, request, ct));

    private long UserId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);
    private long? UserIdOrNull => long.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;
}
