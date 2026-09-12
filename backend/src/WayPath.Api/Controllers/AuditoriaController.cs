using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Policy = "permission:AUDITORIA_LEER")]
public sealed class AuditoriaController(AuditoriaService service) : ControllerBase
{
    [HttpGet("cambios")]
    public async Task<IActionResult> Cambios([FromQuery] long? usuarioId, [FromQuery] string? esquema,
        [FromQuery] string? tabla, [FromQuery] string? operacion, [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta, [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await service.Changes(usuarioId, esquema, tabla, operacion, desde, hasta, page, pageSize, ct));

    [HttpGet("cambios/{id:long}")]
    public async Task<IActionResult> Cambio(long id, CancellationToken ct)
    {
        var item = await service.Change(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("accesos")]
    public async Task<IActionResult> Accesos([FromQuery] long? usuarioId, [FromQuery] string? resultado,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await service.Accesses(usuarioId, resultado, desde, hasta, page, pageSize, ct));

    [HttpGet("errores")]
    public async Task<IActionResult> Errores([FromQuery] string? nivel, [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta, [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await service.Errors(nivel, desde, hasta, page, pageSize, ct));

    [HttpGet("errores/{id:long}")]
    public async Task<IActionResult> Error(long id, CancellationToken ct)
    {
        var item = await service.Error(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct) => Ok(await service.Summary(ct));

    [HttpGet("actividad-reciente")]
    public async Task<IActionResult> ActividadReciente(CancellationToken ct) => Ok(await service.RecentActivity(ct));
}
