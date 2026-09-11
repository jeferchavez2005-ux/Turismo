namespace WayPath.Application;

public sealed class PagoService(IPagoStore store)
{
    private static readonly HashSet<string> GatewayResults = ["APROBADO", "RECHAZADO", "ERROR"];

    public Task<MetodoPagoDto[]> Methods(CancellationToken ct) => store.Methods(ct);
    public Task<OrdenPagoDto> CreateOrder(long usuarioId, CrearOrdenPagoRequest request, CancellationToken ct) =>
        store.CreateOrder(usuarioId, request, ct);
    public Task<PagoDto> RegisterAttempt(long usuarioId, RegistrarIntentoPagoRequest request, CancellationToken ct) =>
        store.RegisterAttempt(usuarioId, Clean(request), ct);
    public Task<EstadoPagoDto> Status(long usuarioId, long reservaId, CancellationToken ct) =>
        store.Status(usuarioId, reservaId, ct);
    public Task<PagoDto[]> Mine(long usuarioId, CancellationToken ct) => store.Mine(usuarioId, ct);
    public Task<PagoPage> ListPayments(int page, int pageSize, CancellationToken ct) =>
        store.ListPayments(page, pageSize, ct);
    public Task<OrdenPagoPage> ListOrders(int page, int pageSize, CancellationToken ct) =>
        store.ListOrders(page, pageSize, ct);
    public Task ChangePaymentState(long pagoId, CambiarEstadoPagoRequest request, long actorId, CancellationToken ct) =>
        store.ChangePaymentState(pagoId, Clean(request), actorId, ct);

    private static RegistrarIntentoPagoRequest Clean(RegistrarIntentoPagoRequest request)
    {
        var result = Required(request.ResultadoSimulado, "El resultado de pago es obligatorio.").ToUpperInvariant();
        if (!GatewayResults.Contains(result)) throw new SecurityFault(400, "Resultado de pasarela no valido.");
        return request with { ResultadoSimulado = result, Mensaje = Optional(request.Mensaje) };
    }

    private static CambiarEstadoPagoRequest Clean(CambiarEstadoPagoRequest request) => request with
    {
        Codigo = Required(request.Codigo, "El estado es obligatorio.").ToUpperInvariant(),
        Observaciones = Optional(request.Observaciones)
    };

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
