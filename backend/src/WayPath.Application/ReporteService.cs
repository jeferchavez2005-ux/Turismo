namespace WayPath.Application;

public sealed class ReporteService(IReporteStore store)
{
    public Task<KpiDefinicionDto[]> Kpis(CancellationToken ct) => store.Kpis(ct);
    public Task<MetricaDiariaPage> DailyMetrics(DateOnly? from, DateOnly? to, string? metricName,
        string? dimensionName, int page, int pageSize, CancellationToken ct) =>
        store.DailyMetrics(from, to, OptionalUpper(metricName), OptionalUpper(dimensionName), page, pageSize, ct);
    public Task<int> GenerateDailyMetrics(DateOnly date, CancellationToken ct) => store.GenerateDailyMetrics(date, ct);
    public Task<DashboardMetricasComercialesDto> Commercial(CancellationToken ct) => store.Commercial(ct);
    public Task<DashboardReservasDto> Reservations(CancellationToken ct) => store.Reservations(ct);
    public Task<DashboardPagosDto> Payments(CancellationToken ct) => store.Payments(ct);
    public Task<DashboardOperacionDto> Operation(CancellationToken ct) => store.Operation(ct);
    public Task<ReporteClientesDto> Clients(CancellationToken ct) => store.Clients(ct);
    public Task<DashboardSerieDto[]> Reviews(CancellationToken ct) => store.Reviews(ct);
    public Task<EventoAnaliticaDto> CreateEvent(long? userId, string? sessionId, string? ip,
        string? userAgent, EventoAnaliticaRequest request, CancellationToken ct) =>
        store.CreateEvent(userId, Optional(sessionId), ip, Optional(userAgent), Clean(request), ct);
    public Task<EventoAnaliticaPage> Events(string? type, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct) => store.Events(OptionalUpper(type), from, to, page, pageSize, ct);
    public Task<EventoResumenDto[]> EventSummary(CancellationToken ct) => store.EventSummary(ct);
    public Task<DashboardSnapshotPage> Snapshots(string? dashboardCode, int page, int pageSize, CancellationToken ct) =>
        store.Snapshots(OptionalUpper(dashboardCode), page, pageSize, ct);
    public Task<DashboardSnapshotDto?> Snapshot(long id, CancellationToken ct) => store.Snapshot(id, ct);
    public Task<DashboardSnapshotDto> CreateSnapshot(long userId, DashboardSnapshotRequest request, CancellationToken ct) =>
        store.CreateSnapshot(userId, Clean(request), ct);

    private static EventoAnaliticaRequest Clean(EventoAnaliticaRequest request) => request with
    {
        TipoEvento = Required(request.TipoEvento, "El tipo de evento es obligatorio.").ToUpperInvariant(),
        Pagina = Optional(request.Pagina),
        RecursoId = Optional(request.RecursoId)
    };

    private static DashboardSnapshotRequest Clean(DashboardSnapshotRequest request) => request with
    {
        CodigoDashboard = Required(request.CodigoDashboard, "El codigo del dashboard es obligatorio.").ToUpperInvariant(),
        Titulo = Required(request.Titulo, "El titulo del snapshot es obligatorio.")
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

    private static string? OptionalUpper(string? value) => Optional(value)?.ToUpperInvariant();
}
