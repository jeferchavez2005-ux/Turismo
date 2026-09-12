using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WayPath.Application;

public sealed record KpiDefinicionDto(int KpiDefinicionId, string Codigo, string Nombre,
    string? Descripcion, string? Formula, bool EsActivo);

public sealed record MetricaDiariaDto(string MetricaDiariaId, int? KpiDefinicionId, DateOnly Fecha,
    string NombreMetrica, string? DimensionNombre, string? DimensionValor, decimal ValorDecimal, DateTime CreadoEn);

public sealed record EventoAnaliticaRequest(
    [Required, StringLength(80)] string TipoEvento,
    [StringLength(250)] string? Pagina,
    [StringLength(80)] string? RecursoId,
    JsonElement? Datos);

public sealed record EventoAnaliticaDto(string EventoAnaliticaId, string? UsuarioId, string? ClienteId,
    string? SesionId, string TipoEvento, string? Pagina, string? RecursoId, string? DatosJson,
    string? Ip, string? UserAgent, DateTime CreadoEn);

public sealed record EventoResumenDto(string TipoEvento, int Total, DateTime? UltimoEventoEn);

public sealed record DashboardSnapshotRequest(
    [Required, StringLength(80)] string CodigoDashboard,
    [Required, StringLength(160)] string Titulo,
    [Required] JsonElement Datos);

public sealed record DashboardSnapshotDto(string DashboardSnapshotId, string CodigoDashboard, string Titulo,
    string DatosJson, string? GeneradoPorUsuarioId, DateTime GeneradoEn);
public sealed record ReporteClientesDto(int TotalClientes, int ClientesActivos,
    DashboardSerieDto[] NuevosPorMes, DashboardSerieDto[] PorEstado);

public sealed record MetricaDiariaPage(MetricaDiariaDto[] Items, int Page, int PageSize, int Total);
public sealed record EventoAnaliticaPage(EventoAnaliticaDto[] Items, int Page, int PageSize, int Total);
public sealed record DashboardSnapshotPage(DashboardSnapshotDto[] Items, int Page, int PageSize, int Total);

public interface IReporteStore
{
    Task<KpiDefinicionDto[]> Kpis(CancellationToken ct);
    Task<MetricaDiariaPage> DailyMetrics(DateOnly? from, DateOnly? to, string? metricName,
        string? dimensionName, int page, int pageSize, CancellationToken ct);
    Task<int> GenerateDailyMetrics(DateOnly date, CancellationToken ct);
    Task<DashboardMetricasComercialesDto> Commercial(CancellationToken ct);
    Task<DashboardReservasDto> Reservations(CancellationToken ct);
    Task<DashboardPagosDto> Payments(CancellationToken ct);
    Task<DashboardOperacionDto> Operation(CancellationToken ct);
    Task<ReporteClientesDto> Clients(CancellationToken ct);
    Task<DashboardSerieDto[]> Reviews(CancellationToken ct);
    Task<EventoAnaliticaDto> CreateEvent(long? userId, string? sessionId, string? ip,
        string? userAgent, EventoAnaliticaRequest request, CancellationToken ct);
    Task<EventoAnaliticaPage> Events(string? type, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct);
    Task<EventoResumenDto[]> EventSummary(CancellationToken ct);
    Task<DashboardSnapshotPage> Snapshots(string? dashboardCode, int page, int pageSize, CancellationToken ct);
    Task<DashboardSnapshotDto?> Snapshot(long id, CancellationToken ct);
    Task<DashboardSnapshotDto> CreateSnapshot(long userId, DashboardSnapshotRequest request, CancellationToken ct);
}
