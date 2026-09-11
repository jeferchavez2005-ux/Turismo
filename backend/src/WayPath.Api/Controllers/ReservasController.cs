using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize]
public sealed class ReservasController(ReservaService service) : ControllerBase
{
    private const string ManagePolicy = "permission:VENTAS_RESERVAS_GESTIONAR";
    private const string ReadPolicy = "permission:VENTAS_RESERVAS_LEER";

    [HttpPost]
    public async Task<IActionResult> Create(CrearReservaRequest request, CancellationToken ct) =>
        StatusCode(201, await service.Create(UserId, request, ct));

    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok(await service.Mine(UserId, ct));

    [HttpGet("me/{id:long}")]
    public async Task<IActionResult> MyDetail(long id, CancellationToken ct)
    {
        var reserva = await service.DetailForUser(UserId, id, ct);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpPut("me/{id:long}/cancelar")]
    public async Task<IActionResult> CancelMine(long id, CancellationToken ct)
    {
        await service.CancelMine(UserId, id, ct);
        return NoContent();
    }

    [HttpPut("me/{id:long}/pasajeros")]
    public async Task<IActionResult> UpdatePassengers(long id, ActualizarPasajerosReservaRequest request,
        CancellationToken ct) => Ok(await service.UpdatePassengers(UserId, id, request, ct));

    [HttpGet("me/pasajeros")]
    public async Task<IActionResult> MyPassengers(CancellationToken ct) =>
        Ok(await service.MyPassengers(UserId, ct));

    [HttpPost("me/pasajeros")]
    public async Task<IActionResult> CreatePassenger(PasajeroReservaRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreatePassenger(UserId, request, ct));

    [HttpGet, Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> List([FromQuery] long? clienteId, [FromQuery] long? salidaId,
        [FromQuery] int? estadoId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.List(new(clienteId, salidaId, estadoId, desde, hasta), page, pageSize, ct));

    [HttpGet("{id:long}"), Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var reserva = await service.DetailAdmin(id, ct);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpPut("{id:long}/estado"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> ChangeState(long id, CambiarEstadoReservaRequest request, CancellationToken ct)
    {
        await service.ChangeState(id, request, UserId, ct);
        return NoContent();
    }

    private long UserId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);
}
