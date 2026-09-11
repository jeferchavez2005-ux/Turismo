using System.ComponentModel.DataAnnotations;

namespace WayPath.Application;

public sealed record CrearOrdenPagoRequest([Range(1, long.MaxValue)] long ReservaId);

public sealed record RegistrarIntentoPagoRequest(
    [Range(1, long.MaxValue)] long OrdenPagoId,
    [Range(1, int.MaxValue)] int MetodoPagoId,
    [Required, RegularExpression("^(APROBADO|RECHAZADO|ERROR)$")] string ResultadoSimulado,
    [StringLength(500)] string? Mensaje);

public sealed record CambiarEstadoPagoRequest(
    [Required] string Codigo,
    [StringLength(500)] string? Observaciones);

public sealed record MetodoPagoDto(int MetodoPagoId, string Codigo, string Nombre, bool RequierePasarela);
public sealed record OrdenPagoDto(string OrdenPagoId, string ReservaId, string CodigoOrden,
    decimal MontoTotal, string Moneda, string Estado, DateTime FechaCreacion, DateTime? FechaExpiracion);
public sealed record PagoDto(string PagoId, string OrdenPagoId, string ReservaId,
    string MetodoPago, string Estado, decimal Monto, string Moneda, string? CodigoTransaccion,
    string? ReferenciaPasarela, DateTime? FechaPago, DateTime CreadoEn, string? Observaciones);
public sealed record PagoIntentoDto(string PagoIntentoId, string OrdenPagoId, string? PagoId,
    string MetodoPago, string Resultado, string? Mensaje, DateTime CreadoEn);
public sealed record EstadoPagoDto(string ReservaId, string CodigoReserva, string EstadoReserva,
    OrdenPagoDto? OrdenActiva, PagoDto? UltimoPago, PagoIntentoDto[] Intentos);
public sealed record PagoPage(PagoDto[] Items, int Page, int PageSize, int Total);
public sealed record OrdenPagoPage(OrdenPagoDto[] Items, int Page, int PageSize, int Total);

public interface IPagoStore
{
    Task<MetodoPagoDto[]> Methods(CancellationToken ct);
    Task<OrdenPagoDto> CreateOrder(long usuarioId, CrearOrdenPagoRequest request, CancellationToken ct);
    Task<PagoDto> RegisterAttempt(long usuarioId, RegistrarIntentoPagoRequest request, CancellationToken ct);
    Task<EstadoPagoDto> Status(long usuarioId, long reservaId, CancellationToken ct);
    Task<PagoDto[]> Mine(long usuarioId, CancellationToken ct);
    Task<PagoPage> ListPayments(int page, int pageSize, CancellationToken ct);
    Task<OrdenPagoPage> ListOrders(int page, int pageSize, CancellationToken ct);
    Task ChangePaymentState(long pagoId, CambiarEstadoPagoRequest request, long actorId, CancellationToken ct);
}
