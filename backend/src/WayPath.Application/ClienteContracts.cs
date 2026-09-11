using System.ComponentModel.DataAnnotations;

namespace WayPath.Application;

public sealed record ClientePerfilRequest(
    [Required, StringLength(120)] string Nombres,
    [Required, StringLength(120)] string Apellidos,
    [StringLength(30)] string? Telefono,
    DateOnly? FechaNacimiento,
    [RegularExpression("^(MASCULINO|FEMENINO|OTRO|NO_DECLARA)$")] string? Genero);

public sealed record DocumentoClienteRequest(
    [Range(1, int.MaxValue)] int TipoDocumentoId,
    [Required, StringLength(40)] string NumeroDocumento,
    int? PaisEmisionId,
    DateOnly? FechaVencimiento,
    bool EsPrincipal);

public sealed record DireccionClienteRequest(
    [Range(1, int.MaxValue)] int CiudadId,
    [Required, StringLength(250)] string LineaDireccion,
    [StringLength(20)] string? CodigoPostal,
    bool EsPrincipal);

public sealed record PreferenciaClienteRequest(
    [Required, StringLength(10)] string IdiomaPreferido,
    int? MonedaPreferidaId,
    bool RecibePromociones,
    bool RequiereAccesibilidad,
    [StringLength(500)] string? Observaciones);

public sealed record ClientePerfilDto(string ClienteId, string UsuarioId, string Email,
    string? NombreUsuario, string Estado, string Nombres, string Apellidos, string? Telefono,
    DateOnly? FechaNacimiento, string? Genero, string Version);

public sealed record ClienteResumenDto(string ClienteId, string UsuarioId, string Email,
    string Estado, string Nombres, string Apellidos, string? Telefono, DateTime CreadoEn);

public sealed record DocumentoClienteDto(string DocumentoClienteId, int TipoDocumentoId,
    string TipoDocumento, string NumeroDocumento, int? PaisEmisionId, string? PaisEmision,
    DateOnly? FechaVencimiento, bool EsPrincipal);

public sealed record DireccionClienteDto(string DireccionClienteId, int CiudadId,
    string Ciudad, string Region, string Pais, string LineaDireccion, string? CodigoPostal,
    bool EsPrincipal);

public sealed record PreferenciaClienteDto(string PreferenciaClienteId, string IdiomaPreferido,
    int? MonedaPreferidaId, string? MonedaPreferida, bool RecibePromociones,
    bool RequiereAccesibilidad, string? Observaciones);

public sealed record ClienteCatalogosDto(TipoDocumentoDto[] TiposDocumento,
    PaisDto[] Paises, CiudadDto[] Ciudades, MonedaDto[] Monedas);
public sealed record TipoDocumentoDto(int TipoDocumentoId, string Codigo, string Nombre,
    int? LongitudMinima, int? LongitudMaxima);
public sealed record PaisDto(int PaisId, string CodigoIso2, string Nombre);
public sealed record CiudadDto(int CiudadId, string Nombre, string Region, string Pais);
public sealed record MonedaDto(int MonedaId, string CodigoIso, string Nombre, string Simbolo,
    bool EsMonedaBase);
public sealed record ClientePage(ClienteResumenDto[] Items, int Page, int PageSize, int Total);

public interface IClienteStore
{
    Task<ClientePerfilDto?> GetByUser(long usuarioId, CancellationToken ct);
    Task<ClientePerfilDto> UpsertForUser(long usuarioId, ClientePerfilRequest request, CancellationToken ct);
    Task<ClientePage> List(int page, int pageSize, CancellationToken ct);
    Task SetState(long clienteId, string codigo, CancellationToken ct);
    Task<DocumentoClienteDto[]> ListDocuments(long usuarioId, CancellationToken ct);
    Task<DocumentoClienteDto> AddDocument(long usuarioId, DocumentoClienteRequest request, CancellationToken ct);
    Task<DocumentoClienteDto> UpdateDocument(long usuarioId, long documentId, DocumentoClienteRequest request, CancellationToken ct);
    Task DeleteDocument(long usuarioId, long documentId, CancellationToken ct);
    Task<DireccionClienteDto[]> ListAddresses(long usuarioId, CancellationToken ct);
    Task<DireccionClienteDto> AddAddress(long usuarioId, DireccionClienteRequest request, CancellationToken ct);
    Task<DireccionClienteDto> UpdateAddress(long usuarioId, long addressId, DireccionClienteRequest request, CancellationToken ct);
    Task DeleteAddress(long usuarioId, long addressId, CancellationToken ct);
    Task<PreferenciaClienteDto?> GetPreferences(long usuarioId, CancellationToken ct);
    Task<PreferenciaClienteDto> UpsertPreferences(long usuarioId, PreferenciaClienteRequest request, CancellationToken ct);
    Task<ClienteCatalogosDto> Catalogs(CancellationToken ct);
}
