using System.ComponentModel.DataAnnotations;

namespace WayPath.Application;

public sealed record CatalogoFiltroRequest(string? Search, int? CiudadId, int? CategoriaId,
    decimal? PrecioMinimo, decimal? PrecioMaximo, bool? Destacado);

public sealed record DestinoRequest(
    [Range(1, int.MaxValue)] int CiudadId,
    long? UbicacionTuristicaId,
    [Required, StringLength(160)] string Nombre,
    [Required, StringLength(180)] string Slug,
    [Required, StringLength(300)] string DescripcionCorta,
    string? DescripcionLarga,
    [Range(0, int.MaxValue)] int? AltitudMetros,
    bool EsDestacado,
    [Required] int[] CategoriaIds);

public sealed record PaqueteRequest(
    [Range(1, long.MaxValue)] long DestinoId,
    [Required, StringLength(180)] string Nombre,
    [Required, StringLength(200)] string Slug,
    string? Descripcion,
    [Range(1, int.MaxValue)] int DuracionDias,
    [Range(0, int.MaxValue)] int DuracionNoches,
    [RegularExpression("^(BAJO|MEDIO|ALTO|EXTREMO)$")] string? NivelDificultad,
    bool EsDestacado);

public sealed record DestinoResumenDto(string DestinoId, string Nombre, string Slug,
    string DescripcionCorta, string? ImagenUrl, string? ImagenAlt, string Ciudad,
    string Region, string Pais, bool EsDestacado, decimal? PrecioDesde, string? Moneda,
    CategoriaDto[] Categorias);

public sealed record DestinoDetalleDto(string DestinoId, string Nombre, string Slug,
    string DescripcionCorta, string? DescripcionLarga, string? ImagenUrl, string? ImagenAlt,
    string Ciudad, string Region, string Pais, int? AltitudMetros, bool EsDestacado,
    CategoriaDto[] Categorias, AtraccionDto[] Atracciones, PaqueteResumenDto[] Paquetes);

public sealed record PaqueteResumenDto(string PaqueteId, string DestinoId, string Nombre,
    string Slug, int DuracionDias, int DuracionNoches, string? NivelDificultad,
    bool EsDestacado, decimal? PrecioDesde, string? Moneda);

public sealed record PaqueteDetalleDto(string PaqueteId, string DestinoId, string Destino,
    string Nombre, string Slug, string? Descripcion, int DuracionDias, int DuracionNoches,
    string? NivelDificultad, bool EsDestacado, TarifaDto[] Tarifas, string[] Incluye,
    string[] NoIncluye, ItinerarioDto[] Itinerario);

public sealed record CategoriaDto(int CategoriaDestinoId, string Codigo, string Nombre);
public sealed record AtraccionDto(string AtraccionDestinoId, string Nombre,
    string? Descripcion, int? DuracionEstimadaMinutos);
public sealed record TarifaDto(string TarifaId, string TipoTarifa, decimal PrecioBase,
    decimal? PrecioNino, string Moneda, string Simbolo, DateOnly VigenciaInicio,
    DateOnly? VigenciaFin);
public sealed record ItinerarioDto(string PaqueteItinerarioId, int DiaNumero,
    string Titulo, string Descripcion);

public sealed record CatalogoOpcionesDto(CategoriaDto[] Categorias, CiudadDto[] Ciudades);

public interface ICatalogoStore
{
    Task<DestinoResumenDto[]> ListDestinos(CatalogoFiltroRequest filtro, CancellationToken ct);
    Task<DestinoDetalleDto?> GetDestino(string slugOrId, CancellationToken ct);
    Task<DestinoDetalleDto> CreateDestino(DestinoRequest request, CancellationToken ct);
    Task<DestinoDetalleDto> UpdateDestino(long destinoId, DestinoRequest request, CancellationToken ct);
    Task SetDestinoState(long destinoId, string codigo, CancellationToken ct);
    Task<PaqueteResumenDto[]> ListPaquetes(long? destinoId, CancellationToken ct);
    Task<PaqueteDetalleDto?> GetPaquete(string slugOrId, CancellationToken ct);
    Task<PaqueteDetalleDto> CreatePaquete(PaqueteRequest request, CancellationToken ct);
    Task<PaqueteDetalleDto> UpdatePaquete(long paqueteId, PaqueteRequest request, CancellationToken ct);
    Task SetPaqueteState(long paqueteId, string codigo, CancellationToken ct);
    Task<CatalogoOpcionesDto> Options(CancellationToken ct);
}
