using System.ComponentModel.DataAnnotations;

namespace WayPath.Application;

public sealed record DisponibilidadFiltroRequest(long? PaqueteId, DateOnly? Desde, DateOnly? Hasta);
public sealed record SalidasFiltroRequest(long? PaqueteId, int? EstadoId, DateOnly? Desde, DateOnly? Hasta);

public sealed record SalidaRequest(
    [Range(1, long.MaxValue)] long PaqueteId,
    [Required] DateOnly FechaInicio,
    [Required] DateOnly FechaFin,
    [Range(0, int.MaxValue)] int CupoTotal,
    [Range(0, int.MaxValue)] int CupoReservado,
    [StringLength(250)] string? PuntoEncuentro);

public sealed record CupoSalidaRequest(
    [Required, StringLength(40)] string TipoCupo,
    [Range(0, int.MaxValue)] int CupoTotal,
    [Range(0, int.MaxValue)] int CupoReservado);

public sealed record ProveedorRequest(
    int? TipoDocumentoId,
    [StringLength(40)] string? NumeroDocumento,
    [Required, StringLength(180)] string RazonSocial,
    [StringLength(180)] string? NombreComercial,
    [EmailAddress, StringLength(255)] string? Email,
    [StringLength(30)] string? Telefono,
    int? CiudadId);

public sealed record ProveedorServicioRequest(
    [Range(1, long.MaxValue)] long ProveedorId,
    [Range(1, int.MaxValue)] int MonedaId,
    [Required, RegularExpression("^(HOTEL|TRANSPORTE|GUIA|ENTRADA|ALIMENTACION|SEGURO|OTRO)$")] string TipoServicio,
    [Required, StringLength(160)] string Nombre,
    [StringLength(500)] string? Descripcion,
    [Range(0, double.MaxValue)] decimal CostoBase);

public sealed record AsignacionOperacionRequest(
    [Range(1, long.MaxValue)] long SalidaId,
    long? ProveedorServicioId,
    long? GuiaId,
    long? TransporteId,
    long? HotelId,
    long? HabitacionId,
    [StringLength(500)] string? Observaciones);

public sealed record BloqueoDisponibilidadRequest(
    long? PaqueteId,
    long? SalidaId,
    [Required] DateOnly FechaInicio,
    [Required] DateOnly FechaFin,
    [Required, StringLength(250)] string Motivo);

public sealed record ItinerarioOperativoRequest(
    [Range(1, long.MaxValue)] long SalidaId,
    [Range(1, int.MaxValue)] int DiaNumero,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFin,
    [Required, StringLength(180)] string Actividad,
    [StringLength(500)] string? Descripcion);

public sealed record DisponibilidadDto(string SalidaId, string PaqueteId, string Paquete,
    string Destino, DateOnly FechaInicio, DateOnly FechaFin, int CupoTotal,
    int CupoReservado, int CupoDisponible, bool EstaBloqueada, string Estado,
    CupoSalidaDto[] Cupos);

public sealed record SalidaDto(string SalidaId, string PaqueteId, string Paquete,
    DateOnly FechaInicio, DateOnly FechaFin, int CupoTotal, int CupoReservado,
    int CupoDisponible, string? PuntoEncuentro, string Estado);
public sealed record CupoSalidaDto(string CupoSalidaId, string TipoCupo, int CupoTotal,
    int CupoReservado, int CupoDisponible);
public sealed record ProveedorDto(string ProveedorId, string RazonSocial,
    string? NombreComercial, string? Email, string? Telefono, string? Ciudad, string Estado);
public sealed record ProveedorServicioDto(string ProveedorServicioId, string ProveedorId,
    string Proveedor, string TipoServicio, string Nombre, string? Descripcion,
    decimal CostoBase, string Moneda, string Estado);
public sealed record RecursoOperativoDto(string Tipo, string Id, string Nombre, string? Detalle,
    int? Capacidad, decimal? Costo, string Estado);
public sealed record AsignacionOperacionDto(string AsignacionOperacionId, string SalidaId,
    string? ProveedorServicio, string? Guia, string? Transporte, string? Hotel,
    string? Habitacion, DateTime FechaAsignacion, string? Observaciones);
public sealed record BloqueoDisponibilidadDto(string BloqueoDisponibilidadId, string? PaqueteId,
    string? SalidaId, DateOnly FechaInicio, DateOnly FechaFin, string Motivo,
    string? CreadoPorUsuarioId, DateTime CreadoEn);
public sealed record ItinerarioOperativoDto(string ItinerarioOperativoId, string SalidaId,
    int DiaNumero, TimeOnly? HoraInicio, TimeOnly? HoraFin, string Actividad,
    string? Descripcion);
public sealed record OperacionOpcionesDto(EstadoOpcionDto[] EstadosSalida,
    EstadoOpcionDto[] EstadosProveedor, EstadoOpcionDto[] EstadosRecurso, AreaDto[] Areas);
public sealed record EstadoOpcionDto(int EstadoId, string Grupo, string Codigo, string Nombre);
public sealed record AreaDto(int AreaId, string Codigo, string Nombre);

public interface IOperacionStore
{
    Task<DisponibilidadDto[]> Disponibilidad(DisponibilidadFiltroRequest filtro, CancellationToken ct);
    Task<SalidaDto[]> ListSalidas(SalidasFiltroRequest filtro, CancellationToken ct);
    Task<SalidaDto?> GetSalida(long salidaId, CancellationToken ct);
    Task<SalidaDto> CreateSalida(SalidaRequest request, CancellationToken ct);
    Task<SalidaDto> UpdateSalida(long salidaId, SalidaRequest request, CancellationToken ct);
    Task SetSalidaState(long salidaId, string codigo, CancellationToken ct);
    Task<CupoSalidaDto[]> ListCupos(long salidaId, CancellationToken ct);
    Task<CupoSalidaDto> UpsertCupo(long salidaId, CupoSalidaRequest request, CancellationToken ct);
    Task<ProveedorDto[]> ListProveedores(CancellationToken ct);
    Task<ProveedorDto> CreateProveedor(ProveedorRequest request, CancellationToken ct);
    Task<ProveedorDto> UpdateProveedor(long proveedorId, ProveedorRequest request, CancellationToken ct);
    Task SetProveedorState(long proveedorId, string codigo, CancellationToken ct);
    Task<ProveedorServicioDto[]> ListProveedorServicios(long? proveedorId, CancellationToken ct);
    Task<ProveedorServicioDto> CreateProveedorServicio(ProveedorServicioRequest request, CancellationToken ct);
    Task<ProveedorServicioDto> UpdateProveedorServicio(long proveedorServicioId, ProveedorServicioRequest request, CancellationToken ct);
    Task<RecursoOperativoDto[]> Recursos(CancellationToken ct);
    Task<AsignacionOperacionDto[]> ListAsignaciones(long salidaId, CancellationToken ct);
    Task<AsignacionOperacionDto> CreateAsignacion(AsignacionOperacionRequest request, CancellationToken ct);
    Task<BloqueoDisponibilidadDto[]> ListBloqueos(long? paqueteId, long? salidaId, CancellationToken ct);
    Task<BloqueoDisponibilidadDto> CreateBloqueo(BloqueoDisponibilidadRequest request, long? actorId, CancellationToken ct);
    Task<ItinerarioOperativoDto[]> ListItinerario(long salidaId, CancellationToken ct);
    Task<ItinerarioOperativoDto> CreateItinerario(ItinerarioOperativoRequest request, CancellationToken ct);
    Task<OperacionOpcionesDto> Options(CancellationToken ct);
}
