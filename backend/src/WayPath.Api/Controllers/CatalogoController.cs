using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public sealed class CatalogoController(CatalogoService service, IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxDestinationImageBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, (string Extension, byte[][] Signatures)> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = (".jpg", [[0xFF, 0xD8, 0xFF]]),
        ["image/png"] = (".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
        ["image/webp"] = (".webp", [[0x52, 0x49, 0x46, 0x46]])
    };
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

    [HttpGet("admin/destinos"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> DestinosAdmin([FromQuery] string? search,
        [FromQuery] int? ciudadId, [FromQuery] int? categoriaId,
        [FromQuery] decimal? precioMinimo, [FromQuery] decimal? precioMaximo,
        [FromQuery] bool? destacado, CancellationToken ct) =>
        Ok(await service.DestinosAdmin(new(search, ciudadId, categoriaId, precioMinimo,
            precioMaximo, destacado), ct));

    [HttpGet("admin/destinos/{slugOrId}"), Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    public async Task<IActionResult> DestinoAdmin(string slugOrId, CancellationToken ct)
    {
        var destino = await service.DestinoAdmin(slugOrId, ct);
        return destino is null ? NotFound() : Ok(destino);
    }

    [HttpPost("admin/destinos/{id:long}/imagen")]
    [Authorize(Policy = "permission:CATALOGO_DESTINOS_GESTIONAR")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDestinationImageBytes + 64 * 1024)]
    public async Task<IActionResult> UploadDestinationImage(long id, IFormFile image, CancellationToken ct)
    {
        if (image.Length is <= 0 or > MaxDestinationImageBytes)
            throw new SecurityFault(400, "La imagen debe pesar como máximo 5 MB.");
        if (!ImageTypes.TryGetValue(image.ContentType, out var imageType))
            throw new SecurityFault(400, "Solo se permiten imágenes JPEG, PNG o WebP.");

        await using var input = image.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAsync(header, ct);
        var validHeader = imageType.Signatures.Any(signature => read >= signature.Length
            && header.AsSpan(0, signature.Length).SequenceEqual(signature));
        if (image.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            validHeader = validHeader && read >= 12 && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        if (!validHeader) throw new SecurityFault(400, "El contenido del archivo no coincide con una imagen válida.");

        var storedName = $"{id}-{Guid.NewGuid():N}{imageType.Extension}";
        var frontendRoot = environment.IsDevelopment()
            ? Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../../../Proyecto_Turismo"))
            : environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadRoot = Path.GetFullPath(Path.Combine(frontendRoot, "uploads", "destinos"));
        Directory.CreateDirectory(uploadRoot);
        var destinationPath = Path.GetFullPath(Path.Combine(uploadRoot, storedName));
        if (!destinationPath.StartsWith(uploadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new SecurityFault(400, "Ruta de imagen inválida.");

        input.Position = 0;
        await using (var output = System.IO.File.Create(destinationPath))
            await input.CopyToAsync(output, ct);
        var url = $"uploads/destinos/{storedName}";
        await service.SetDestinoImage(id, url, storedName, image.ContentType, $"Imagen de destino {id}", ct);
        return Ok(new { imagenUrl = url });
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
