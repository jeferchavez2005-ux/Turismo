namespace WayPath.Application;

public sealed record DashboardResumenDto(
    int UsuariosRegistrados,
    int ClientesActivos,
    int ReservasTotales,
    int PagosAprobados,
    decimal TotalVendido,
    int ComprasGeneradas,
    int CuposDisponibles,
    int ResenasPendientes);

public sealed record DashboardMetricasComercialesDto(
    DashboardSerieDto[] IngresosMensuales,
    DashboardSerieDto[] DestinosMasVendidos,
    DashboardSerieDto[] PaquetesMasVendidos,
    DashboardSerieDto[] MetodosPago);

public sealed record DashboardReservasDto(int Total, DashboardSerieDto[] PorEstado, DashboardSerieDto[] PorMes);
public sealed record DashboardPagosDto(int Total, DashboardSerieDto[] PorEstado, DashboardSerieDto[] PorMetodo);
public sealed record DashboardVentasDto(decimal TotalVendido, int ComprasGeneradas,
    DashboardSerieDto[] IngresosMensuales, DashboardSerieDto[] DestinosMasVendidos,
    DashboardSerieDto[] PaquetesMasVendidos);
public sealed record DashboardOperacionDto(int CupoTotal, int CupoReservado, int CupoDisponible,
    DashboardSerieDto[] CuposPorSalida, DashboardSerieDto[] SalidasPorEstado);
public sealed record DashboardSerieDto(string Etiqueta, decimal Valor);

public interface IDashboardStore
{
    Task<DashboardResumenDto> Summary(CancellationToken ct);
    Task<DashboardMetricasComercialesDto> CommercialMetrics(CancellationToken ct);
    Task<DashboardReservasDto> Reservations(CancellationToken ct);
    Task<DashboardPagosDto> Payments(CancellationToken ct);
    Task<DashboardVentasDto> Sales(CancellationToken ct);
    Task<DashboardOperacionDto> Operation(CancellationToken ct);
}
