using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "permission:REPORTES_LEER")]
public sealed class AdminDashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct) => Ok(await service.Summary(ct));

    [HttpGet("metricas-comerciales")]
    public async Task<IActionResult> MetricasComerciales(CancellationToken ct) =>
        Ok(await service.CommercialMetrics(ct));

    [HttpGet("reservas")]
    public async Task<IActionResult> Reservas(CancellationToken ct) => Ok(await service.Reservations(ct));

    [HttpGet("pagos")]
    public async Task<IActionResult> Pagos(CancellationToken ct) => Ok(await service.Payments(ct));

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas(CancellationToken ct) => Ok(await service.Sales(ct));

    [HttpGet("operacion")]
    public async Task<IActionResult> Operacion(CancellationToken ct) => Ok(await service.Operation(ct));
}
