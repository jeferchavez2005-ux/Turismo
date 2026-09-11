using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public sealed class ClientesController(ClienteService service) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var profile = await service.Get(UserId, ct);
        return Ok(new { existe = profile is not null, perfil = profile });
    }

    [HttpPut("me")]
    public async Task<IActionResult> SaveMe(ClientePerfilRequest request, CancellationToken ct) =>
        Ok(await service.Save(UserId, request, ct));

    [HttpGet("catalogos")]
    public async Task<IActionResult> Catalogos(CancellationToken ct) => Ok(await service.Catalogs(ct));

    [HttpGet("me/documentos")]
    public async Task<IActionResult> Documentos(CancellationToken ct) => Ok(await service.Documents(UserId, ct));

    [HttpPost("me/documentos")]
    public async Task<IActionResult> AddDocumento(DocumentoClienteRequest request, CancellationToken ct) =>
        StatusCode(201, await service.AddDocument(UserId, request, ct));

    [HttpPut("me/documentos/{id:long}")]
    public async Task<IActionResult> UpdateDocumento(long id, DocumentoClienteRequest request, CancellationToken ct) =>
        Ok(await service.UpdateDocument(UserId, id, request, ct));

    [HttpDelete("me/documentos/{id:long}")]
    public async Task<IActionResult> DeleteDocumento(long id, CancellationToken ct)
    {
        await service.DeleteDocument(UserId, id, ct);
        return NoContent();
    }

    [HttpGet("me/direcciones")]
    public async Task<IActionResult> Direcciones(CancellationToken ct) => Ok(await service.Addresses(UserId, ct));

    [HttpPost("me/direcciones")]
    public async Task<IActionResult> AddDireccion(DireccionClienteRequest request, CancellationToken ct) =>
        StatusCode(201, await service.AddAddress(UserId, request, ct));

    [HttpPut("me/direcciones/{id:long}")]
    public async Task<IActionResult> UpdateDireccion(long id, DireccionClienteRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAddress(UserId, id, request, ct));

    [HttpDelete("me/direcciones/{id:long}")]
    public async Task<IActionResult> DeleteDireccion(long id, CancellationToken ct)
    {
        await service.DeleteAddress(UserId, id, ct);
        return NoContent();
    }

    [HttpGet("me/preferencias")]
    public async Task<IActionResult> Preferencias(CancellationToken ct) => Ok(await service.Preferences(UserId, ct));

    [HttpPut("me/preferencias")]
    public async Task<IActionResult> SavePreferencias(PreferenciaClienteRequest request, CancellationToken ct) =>
        Ok(await service.SavePreferences(UserId, request, ct));

    [HttpGet, Authorize(Policy = "security.manage")]
    public async Task<IActionResult> Clientes([FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.List(page, pageSize, ct));

    [HttpPut("{id:long}/state"), Authorize(Policy = "security.manage")]
    public async Task<IActionResult> Estado(long id, StateRequest request, CancellationToken ct)
    {
        await service.SetState(id, request.Codigo, ct);
        return NoContent();
    }

    private long UserId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);
}
