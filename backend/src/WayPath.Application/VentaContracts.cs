using System.ComponentModel.DataAnnotations;

namespace WayPath.Application;

public sealed record CrearResenaRequest(
    [Range(1, long.MaxValue)] long ReservaId,
    [Range(1, 5)] byte Calificacion,
    [StringLength(160)] string? Titulo,
    [Required, StringLength(2000)] string Comentario);

public sealed record ModerarResenaRequest(
    [Required, RegularExpression("^(APROBADA|RECHAZADA)$")] string Estado,
    [StringLength(500)] string? Comentario);

public sealed record CompraResumenDto(string CompraId, string CodigoCompra, string ReservaId,
    string CodigoReserva, string Cliente, string Paquete, string Destino, DateOnly FechaInicio,
    DateOnly FechaFin, string Estado, DateTime FechaCompra, decimal TotalPagado);

public sealed record CompraDetalleDto(string CompraId, string CodigoCompra, string ReservaId,
    string CodigoReserva, string Cliente, string Paquete, string Destino, DateOnly FechaInicio,
    DateOnly FechaFin, string Estado, DateTime FechaCompra, decimal TotalPagado,
    ComprobanteDto? Comprobante, TicketViajeDto[] Tickets, VoucherDto? Voucher);

public sealed record ComprobanteDto(string ComprobanteId, string CompraId, string TipoComprobante,
    string Serie, string Numero, string Estado, DateTime FechaEmision, decimal Subtotal,
    decimal Impuestos, decimal Total, ComprobanteLineaDto[] Detalles);

public sealed record ComprobanteLineaDto(string ComprobanteDetalleId, string Descripcion,
    int Cantidad, decimal PrecioUnitario, decimal Importe);

public sealed record TicketViajeDto(string TicketViajeId, string CodigoTicket, string CompraId,
    string ReservaId, string PasajeroId, string Pasajero, string Estado, DateTime EmitidoEn,
    string? UrlDocumento);

public sealed record VoucherDto(string VoucherId, string CodigoVoucher, string CompraId,
    string ReservaId, string Estado, DateTime EmitidoEn, string? UrlDocumento);

public sealed record ResenaDto(string ResenaId, string Cliente, string Destino,
    string? ReservaId, byte Calificacion, string? Titulo, string Comentario,
    string EstadoModeracion, DateTime CreadoEn);

public sealed record CompraPage(CompraResumenDto[] Items, int Page, int PageSize, int Total);
public sealed record ResenaPage(ResenaDto[] Items, int Page, int PageSize, int Total);

public interface IVentaStore
{
    Task<CompraDetalleDto> EmitFromApprovedPayment(long pagoId, CancellationToken ct);
    Task<CompraResumenDto[]> Mine(long usuarioId, CancellationToken ct);
    Task<CompraDetalleDto?> DetailForUser(long usuarioId, long compraId, CancellationToken ct);
    Task<CompraDetalleDto?> DetailAdmin(long compraId, CancellationToken ct);
    Task<TicketViajeDto?> TicketForUser(long usuarioId, long ticketId, CancellationToken ct);
    Task<VoucherDto?> VoucherForUser(long usuarioId, long voucherId, CancellationToken ct);
    Task<CompraPage> List(int page, int pageSize, CancellationToken ct);
    Task SetCompraState(long compraId, string codigo, CancellationToken ct);
    Task SetComprobanteState(long comprobanteId, string codigo, CancellationToken ct);
    Task<ResenaDto[]> PublicReviews(long? destinoId, byte? calificacion, CancellationToken ct);
    Task<ResenaDto> CreateReview(long usuarioId, CrearResenaRequest request, CancellationToken ct);
    Task<ResenaPage> ListReviews(int page, int pageSize, CancellationToken ct);
    Task ModerateReview(long resenaId, ModerarResenaRequest request, long actorId, CancellationToken ct);
}
