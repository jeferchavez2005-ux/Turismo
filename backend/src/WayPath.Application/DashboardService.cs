namespace WayPath.Application;

public sealed class DashboardService(IDashboardStore store)
{
    public Task<DashboardResumenDto> Summary(CancellationToken ct) => store.Summary(ct);
    public Task<DashboardMetricasComercialesDto> CommercialMetrics(CancellationToken ct) =>
        store.CommercialMetrics(ct);
    public Task<DashboardReservasDto> Reservations(CancellationToken ct) => store.Reservations(ct);
    public Task<DashboardPagosDto> Payments(CancellationToken ct) => store.Payments(ct);
    public Task<DashboardVentasDto> Sales(CancellationToken ct) => store.Sales(ct);
    public Task<DashboardOperacionDto> Operation(CancellationToken ct) => store.Operation(ct);
}
