using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/ventas")]
public sealed class VentasController(VentaService service) : ControllerBase
{
    private const string ManagePolicy = "permission:VENTAS_RESERVAS_GESTIONAR";
    private const string ReadPolicy = "permission:VENTAS_RESERVAS_LEER";

    [HttpGet("compras/me"), Authorize]
    public async Task<IActionResult> MisCompras(CancellationToken ct) =>
        Ok(await service.Mine(UserId, ct));

    [HttpGet("compras/me/{id:long}"), Authorize]
    public async Task<IActionResult> MiCompraDetalle(long id, CancellationToken ct)
    {
        var compra = await service.DetailForUser(UserId, id, ct);
        return compra is null ? NotFound() : Ok(compra);
    }

    [HttpGet("tickets/{id:long}"), Authorize]
    public async Task<IActionResult> MiTicket(long id, CancellationToken ct)
    {
        var ticket = await service.TicketForUser(UserId, id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpGet("vouchers/{id:long}"), Authorize]
    public async Task<IActionResult> MiVoucher(long id, CancellationToken ct)
    {
        var voucher = await service.VoucherForUser(UserId, id, ct);
        return voucher is null ? NotFound() : Ok(voucher);
    }

    [HttpGet("admin/compras"), Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> Compras([FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.List(page, pageSize, ct));

    [HttpGet("admin/compras/{id:long}"), Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> CompraDetalle(long id, CancellationToken ct)
    {
        var compra = await service.DetailAdmin(id, ct);
        return compra is null ? NotFound() : Ok(compra);
    }

    [HttpPut("admin/compras/{id:long}/estado"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CambiarEstadoCompra(long id, CambiarEstadoVentaRequest request, CancellationToken ct)
    {
        await service.SetCompraState(id, request.Codigo, ct);
        return NoContent();
    }

    [HttpPut("admin/comprobantes/{id:long}/estado"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> CambiarEstadoComprobante(long id, CambiarEstadoVentaRequest request, CancellationToken ct)
    {
        await service.SetComprobanteState(id, request.Codigo, ct);
        return NoContent();
    }

    [HttpGet("resenas")]
    [AllowAnonymous]
    public async Task<IActionResult> Resenas([FromQuery] long? destinoId, [FromQuery, Range(1, 5)] byte? calificacion,
        CancellationToken ct) => Ok(await service.PublicReviews(destinoId, calificacion, ct));

    [HttpPost("resenas"), Authorize]
    public async Task<IActionResult> CrearResena(CrearResenaRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateReview(UserId, request, ct));

    [HttpGet("admin/resenas"), Authorize(Policy = ReadPolicy)]
    public async Task<IActionResult> ResenasAdmin([FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.ListReviews(page, pageSize, ct));

    [HttpPut("admin/resenas/{id:long}/moderacion"), Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> ModerarResena(long id, ModerarResenaRequest request, CancellationToken ct)
    {
        await service.ModerateReview(id, request, UserId, ct);
        return NoContent();
    }

    private long UserId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record CambiarEstadoVentaRequest([Required, StringLength(40)] string Codigo);
