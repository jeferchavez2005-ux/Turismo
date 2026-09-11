using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize]
public sealed class PagosController(PagoService service) : ControllerBase
{
    private const string ManagePolicy = "permission:PAGOS_GESTIONAR";
    private const string ReadPolicy = "permission:PAGOS_LEER";

    [HttpGet("metodos")]
    public async Task<IActionResult> Metodos(CancellationToken ct) => Ok(await service.Methods(ct));

    [HttpPost("ordenes")]
    public async Task<IActionResult> CrearOrden(CrearOrdenPagoRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateOrder(UserId, request, ct));

    [HttpPost("intentos")]
    public async Task<IActionResult> RegistrarIntento(RegistrarIntentoPagoRequest request, CancellationToken ct) =>
        StatusCode(201, await service.RegisterAttempt(UserId, request, ct));

    [HttpGet("estado/{reservaId:long}")]
    public async Task<IActionResult> Estado(long reservaId, CancellationToken ct) =>
        Ok(await service.Status(UserId, reservaId, ct));

    [HttpGet("me")]
    public async Task<IActionResult> MisPagos(CancellationToken ct) => Ok(await service.Mine(UserId, ct));

    [HttpGet("admin/pagos"), Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> Pagos([FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.ListPayments(page, pageSize, ct));

    [HttpGet("admin/ordenes"), Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> Ordenes([FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.ListOrders(page, pageSize, ct));

    [HttpPut("admin/pagos/{id:long}/estado"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> EstadoPago(long id, CambiarEstadoPagoRequest request, CancellationToken ct)
    {
        await service.ChangePaymentState(id, request, UserId, ct);
        return NoContent();
    }

    private long UserId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);
}
