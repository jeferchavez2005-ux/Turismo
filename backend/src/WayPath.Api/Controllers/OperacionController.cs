using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/operacion")]
public sealed class OperacionController(OperacionService service) : ControllerBase
{
    private const string ManagePolicy = "permission:VENTAS_RESERVAS_GESTIONAR";

    [HttpGet("disponibilidad"), AllowAnonymous]
    public async Task<IActionResult> Disponibilidad([FromQuery] long? paqueteId,
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Ok(await service.Disponibilidad(new(paqueteId, desde, hasta), ct));

    [HttpGet("salidas"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Salidas([FromQuery] long? paqueteId,
        [FromQuery] int? estadoId, [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Ok(await service.Salidas(new(paqueteId, estadoId, desde, hasta), ct));

    [HttpGet("salidas/{id:long}"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Salida(long id, CancellationToken ct)
    {
        var salida = await service.Salida(id, ct);
        return salida is null ? NotFound() : Ok(salida);
    }

    [HttpPost("salidas"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CreateSalida(SalidaRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateSalida(request, ct));

    [HttpPut("salidas/{id:long}"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> UpdateSalida(long id, SalidaRequest request, CancellationToken ct) =>
        Ok(await service.UpdateSalida(id, request, ct));

    [HttpPut("salidas/{id:long}/state"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> StateSalida(long id, StateRequest request, CancellationToken ct)
    {
        await service.SetSalidaState(id, request.Codigo, ct);
        return NoContent();
    }

    [HttpGet("salidas/{id:long}/cupos"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Cupos(long id, CancellationToken ct) =>
        Ok(await service.Cupos(id, ct));

    [HttpPut("salidas/{id:long}/cupos"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> UpsertCupo(long id, CupoSalidaRequest request, CancellationToken ct) =>
        Ok(await service.UpsertCupo(id, request, ct));

    [HttpGet("proveedores"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Proveedores(CancellationToken ct) =>
        Ok(await service.Proveedores(ct));

    [HttpPost("proveedores"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CreateProveedor(ProveedorRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateProveedor(request, ct));

    [HttpPut("proveedores/{id:long}"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> UpdateProveedor(long id, ProveedorRequest request, CancellationToken ct) =>
        Ok(await service.UpdateProveedor(id, request, ct));

    [HttpPut("proveedores/{id:long}/state"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> StateProveedor(long id, StateRequest request, CancellationToken ct)
    {
        await service.SetProveedorState(id, request.Codigo, ct);
        return NoContent();
    }

    [HttpGet("proveedor-servicios"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> ProveedorServicios([FromQuery] long? proveedorId, CancellationToken ct) =>
        Ok(await service.ProveedorServicios(proveedorId, ct));

    [HttpPost("proveedor-servicios"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CreateProveedorServicio(ProveedorServicioRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateProveedorServicio(request, ct));

    [HttpPut("proveedor-servicios/{id:long}"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> UpdateProveedorServicio(long id, ProveedorServicioRequest request, CancellationToken ct) =>
        Ok(await service.UpdateProveedorServicio(id, request, ct));

    [HttpGet("recursos"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Recursos(CancellationToken ct) =>
        Ok(await service.Recursos(ct));

    [HttpGet("salidas/{id:long}/asignaciones"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Asignaciones(long id, CancellationToken ct) =>
        Ok(await service.Asignaciones(id, ct));

    [HttpPost("asignaciones"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CreateAsignacion(AsignacionOperacionRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateAsignacion(request, ct));

    [HttpGet("bloqueos"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Bloqueos([FromQuery] long? paqueteId,
        [FromQuery] long? salidaId, CancellationToken ct) =>
        Ok(await service.Bloqueos(paqueteId, salidaId, ct));

    [HttpPost("bloqueos"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CreateBloqueo(BloqueoDisponibilidadRequest request, CancellationToken ct)
    {
        long.TryParse(User.FindFirstValue("sub"), out var userId);
        return StatusCode(201, await service.CreateBloqueo(request, userId == 0 ? null : userId, ct));
    }

    [HttpGet("salidas/{id:long}/itinerario"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Itinerario(long id, CancellationToken ct) =>
        Ok(await service.Itinerario(id, ct));

    [HttpPost("itinerario"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CreateItinerario(ItinerarioOperativoRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateItinerario(request, ct));

    [HttpGet("opciones"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Opciones(CancellationToken ct) => Ok(await service.Options(ct));
}
