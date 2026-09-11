namespace WayPath.Application;

public sealed class VentaService(IVentaStore store)
{
    public Task<CompraDetalleDto> EmitFromApprovedPayment(long pagoId, CancellationToken ct) =>
        store.EmitFromApprovedPayment(pagoId, ct);
    public Task<CompraResumenDto[]> Mine(long usuarioId, CancellationToken ct) => store.Mine(usuarioId, ct);
    public Task<CompraDetalleDto?> DetailForUser(long usuarioId, long compraId, CancellationToken ct) =>
        store.DetailForUser(usuarioId, compraId, ct);
    public Task<CompraDetalleDto?> DetailAdmin(long compraId, CancellationToken ct) => store.DetailAdmin(compraId, ct);
    public Task<TicketViajeDto?> TicketForUser(long usuarioId, long ticketId, CancellationToken ct) =>
        store.TicketForUser(usuarioId, ticketId, ct);
    public Task<VoucherDto?> VoucherForUser(long usuarioId, long voucherId, CancellationToken ct) =>
        store.VoucherForUser(usuarioId, voucherId, ct);
    public Task<CompraPage> List(int page, int pageSize, CancellationToken ct) => store.List(page, pageSize, ct);
    public Task SetCompraState(long compraId, string codigo, CancellationToken ct) =>
        store.SetCompraState(compraId, CleanCode(codigo), ct);
    public Task SetComprobanteState(long comprobanteId, string codigo, CancellationToken ct) =>
        store.SetComprobanteState(comprobanteId, CleanCode(codigo), ct);
    public Task<ResenaDto[]> PublicReviews(long? destinoId, byte? calificacion, CancellationToken ct) =>
        store.PublicReviews(destinoId, calificacion, ct);
    public Task<ResenaDto> CreateReview(long usuarioId, CrearResenaRequest request, CancellationToken ct) =>
        store.CreateReview(usuarioId, Clean(request), ct);
    public Task<ResenaPage> ListReviews(int page, int pageSize, CancellationToken ct) =>
        store.ListReviews(page, pageSize, ct);
    public Task ModerateReview(long resenaId, ModerarResenaRequest request, long actorId, CancellationToken ct) =>
        store.ModerateReview(resenaId, Clean(request), actorId, ct);

    private static CrearResenaRequest Clean(CrearResenaRequest request) => request with
    {
        Titulo = Optional(request.Titulo),
        Comentario = Required(request.Comentario, "El comentario de la resena es obligatorio.")
    };

    private static ModerarResenaRequest Clean(ModerarResenaRequest request) => request with
    {
        Estado = CleanCode(request.Estado),
        Comentario = Optional(request.Comentario)
    };

    private static string CleanCode(string value) => Required(value, "El estado es obligatorio.").ToUpperInvariant();

    private static string Required(string value, string error)
    {
        var text = value.Trim();
        if (text.Length == 0) throw new SecurityFault(400, error);
        return text;
    }

    private static string? Optional(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
