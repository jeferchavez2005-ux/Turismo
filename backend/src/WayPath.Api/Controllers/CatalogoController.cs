using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public sealed class CatalogoController(CatalogoService service) : ControllerBase
{
    [HttpGet("destinos"), AllowAnonymous]
    public async Task<IActionResult> Destinos([FromQuery] string? search,
        [FromQuery] int? ciudadId, [FromQuery] int? categoriaId,
        [FromQuery] decimal? precioMinimo, [FromQuery] decimal? precioMaximo,
        [FromQuery] bool? destacado, CancellationToken ct) =>
        Ok(await service.Destinos(new(search, ciudadId, categoriaId, precioMinimo,
            precioMaximo, destacado), ct));

    [HttpGet("destinos/{slugOrId}"), AllowAnonymous]
    public async Task<IActionResult> Destino(string slugOrId, CancellationToken ct)
    {
        var destino = await service.Destino(slugOrId, ct);
        return destino is null ? NotFound() : Ok(destino);
    }

    [HttpPost("destinos"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> CreateDestino(DestinoRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateDestino(request, ct));

    [HttpPut("destinos/{id:long}"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> UpdateDestino(long id, DestinoRequest request, CancellationToken ct) =>
        Ok(await service.UpdateDestino(id, request, ct));

    [HttpPut("destinos/{id:long}/state"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> StateDestino(long id, StateRequest request, CancellationToken ct)
    {
        await service.SetDestinoState(id, request.Codigo, ct);
        return NoContent();
    }

    [HttpGet("paquetes"), AllowAnonymous]
    public async Task<IActionResult> Paquetes([FromQuery] long? destinoId, CancellationToken ct) =>
        Ok(await service.Paquetes(destinoId, ct));

    [HttpGet("paquetes/{slugOrId}"), AllowAnonymous]
    public async Task<IActionResult> Paquete(string slugOrId, CancellationToken ct)
    {
        var paquete = await service.Paquete(slugOrId, ct);
        return paquete is null ? NotFound() : Ok(paquete);
    }

    [HttpPost("paquetes"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> CreatePaquete(PaqueteRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreatePaquete(request, ct));

    [HttpPut("paquetes/{id:long}"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> UpdatePaquete(long id, PaqueteRequest request, CancellationToken ct) =>
        Ok(await service.UpdatePaquete(id, request, ct));

    [HttpPut("paquetes/{id:long}/state"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> StatePaquete(long id, StateRequest request, CancellationToken ct)
    {
        await service.SetPaqueteState(id, request.Codigo, ct);
        return NoContent();
    }

    [HttpGet("opciones"), AllowAnonymous]
    public async Task<IActionResult> Opciones(CancellationToken ct) => Ok(await service.Options(ct));
}
