using System.ComponentModel.DataAnnotations;

namespace WayPath.Application;

public sealed record ReservaPasajeroRequest(
    [Range(1, long.MaxValue)] long PasajeroId,
    [Required, RegularExpression("^(ADULTO|NINO|INFANTE|ADULTO_MAYOR)$")] string TipoPasajero,
    bool EsTitular);

public sealed record CrearReservaRequest(
    [Range(1, long.MaxValue)] long SalidaId,
    [Range(1, long.MaxValue)] long TarifaId,
    [Required, MinLength(1), MaxLength(20)] ReservaPasajeroRequest[] Pasajeros,
    [StringLength(500)] string? Observaciones);

public sealed record ActualizarPasajerosReservaRequest(
    [Required, MinLength(1), MaxLength(20)] ReservaPasajeroRequest[] Pasajeros);

public sealed record CambiarEstadoReservaRequest(
    [Required] string Codigo,
    [StringLength(500)] string? Comentario);

public sealed record ReservaFiltroRequest(long? ClienteId, long? SalidaId, int? EstadoId,
    DateOnly? Desde, DateOnly? Hasta);

public sealed record PasajeroReservaRequest(
    [Required, StringLength(120)] string Nombres,
    [Required, StringLength(120)] string Apellidos,
    DateOnly? FechaNacimiento,
    [StringLength(500)] string? RequerimientosEspeciales);

public sealed record ReservaResumenDto(string ReservaId, string CodigoReserva, string ClienteId,
    string Cliente, string SalidaId, string Paquete, string Destino, DateOnly FechaInicio,
    DateOnly FechaFin, string Estado, DateTime FechaReserva, DateTime? FechaExpiracion,
    decimal Total, string Moneda, int Pasajeros);

public sealed record ReservaDetalleDto(string ReservaId, string CodigoReserva, string ClienteId,
    string Cliente, string SalidaId, string PaqueteId, string Paquete, string Destino,
    DateOnly FechaInicio, DateOnly FechaFin, string Estado, DateTime FechaReserva,
    DateTime? FechaExpiracion, decimal Subtotal, decimal Impuestos, decimal Descuento,
    decimal Total, string Moneda, string Simbolo, string? Observaciones,
    ReservaLineaDto[] Detalles, ReservaPasajeroDto[] Pasajeros,
    ReservaHistorialDto[] Historial);

public sealed record ReservaLineaDto(string ReservaDetalleId, string TipoDetalle,
    string Descripcion, int Cantidad, decimal PrecioUnitario, decimal Importe,
    string? PaqueteId, string? TarifaId);

public sealed record ReservaPasajeroDto(string ReservaPasajeroId, string PasajeroId,
    string Nombres, string Apellidos, string TipoPasajero, bool EsTitular);

public sealed record ReservaHistorialDto(string ReservaHistorialEstadoId, string EstadoNuevo,
    string? EstadoAnterior, string? UsuarioId, string? Comentario, DateTime CreadoEn);
public sealed record PasajeroReservaDto(string PasajeroId, string Nombres, string Apellidos,
    DateOnly? FechaNacimiento, string? RequerimientosEspeciales);

public sealed record ReservaPage(ReservaResumenDto[] Items, int Page, int PageSize, int Total);

public interface IReservaStore
{
    Task<ReservaDetalleDto> Create(long usuarioId, CrearReservaRequest request, CancellationToken ct);
    Task<ReservaResumenDto[]> Mine(long usuarioId, CancellationToken ct);
    Task<ReservaDetalleDto?> DetailForUser(long usuarioId, long reservaId, CancellationToken ct);
    Task<ReservaDetalleDto?> DetailAdmin(long reservaId, CancellationToken ct);
    Task CancelMine(long usuarioId, long reservaId, CancellationToken ct);
    Task<ReservaDetalleDto> UpdatePassengers(long usuarioId, long reservaId, ActualizarPasajerosReservaRequest request, CancellationToken ct);
    Task<ReservaPage> List(ReservaFiltroRequest filtro, int page, int pageSize, CancellationToken ct);
    Task ChangeState(long reservaId, CambiarEstadoReservaRequest request, long actorId, CancellationToken ct);
    Task<PasajeroReservaDto[]> MyPassengers(long usuarioId, CancellationToken ct);
    Task<PasajeroReservaDto> CreatePassenger(long usuarioId, PasajeroReservaRequest request, CancellationToken ct);
}
