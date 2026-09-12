using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Reportes;

namespace WayPath.Infrastructure;

public sealed class ReporteStore(WayPathDbContext db, IDashboardStore dashboard) : IReporteStore
{
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<KpiDefinicionDto[]> Kpis(CancellationToken ct) =>
        await db.KpiDefiniciones.AsNoTracking().OrderBy(x => x.Codigo)
            .Select(x => new KpiDefinicionDto(x.KpiDefinicionId, x.Codigo, x.Nombre,
                x.Descripcion, x.Formula, x.EsActivo)).ToArrayAsync(ct);

    public async Task<MetricaDiariaPage> DailyMetrics(DateOnly? from, DateOnly? to, string? metricName,
        string? dimensionName, int page, int pageSize, CancellationToken ct)
    {
        var query = db.MetricasDiarias.AsNoTracking()
            .Where(x => from == null || x.Fecha >= from)
            .Where(x => to == null || x.Fecha <= to)
            .Where(x => metricName == null || x.NombreMetrica == metricName)
            .Where(x => dimensionName == null || x.DimensionNombre == dimensionName);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Fecha).ThenBy(x => x.NombreMetrica)
            .ThenBy(x => x.DimensionNombre).ThenBy(x => x.DimensionValor)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new MetricaDiariaDto(Id(x.MetricaDiariaId), x.KpiDefinicionId, x.Fecha,
                x.NombreMetrica, x.DimensionNombre, x.DimensionValor, x.ValorDecimal, x.CreadoEn))
            .ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<int> GenerateDailyMetrics(DateOnly date, CancellationToken ct)
    {
        var rows = new List<MetricaSeed>
        {
            new("VENTAS_TOTAL_DIA", null, null, await SalesForDay(date, ct)),
            new("COMPRAS_EMITIDAS_DIA", null, null, await CountComprasForDay(date, ct)),
            new("RESERVAS_CREADAS_DIA", null, null, await CountReservasForDay(date, ct)),
            new("CLIENTES_NUEVOS_DIA", null, null, await CountClientesForDay(date, ct)),
            new("CUPOS_DISPONIBLES_DIA", null, null, await AvailableCapacity(ct)),
            new("RESENAS_PENDIENTES_DIA", null, null, await PendingReviews(ct))
        };
        rows.AddRange(await ReservationsByStateForDay(date, ct));
        rows.AddRange(await PaymentsByStateForDay(date, ct));
        rows.AddRange(await TopDestinationsForDay(date, ct));

        var kpis = await db.KpiDefiniciones.AsNoTracking().ToDictionaryAsync(x => x.Codigo, x => x.KpiDefinicionId, ct);
        var changed = 0;
        foreach (var row in rows)
        {
            var existing = await db.MetricasDiarias.SingleOrDefaultAsync(x => x.Fecha == date
                && x.NombreMetrica == row.Name && x.DimensionNombre == row.DimensionName
                && x.DimensionValor == row.DimensionValue, ct);
            if (existing is null)
            {
                db.MetricasDiarias.Add(new MetricaDiaria
                {
                    KpiDefinicionId = row.KpiCode is not null && kpis.TryGetValue(row.KpiCode, out var kpiId) ? kpiId : null,
                    Fecha = date,
                    NombreMetrica = row.Name,
                    DimensionNombre = row.DimensionName,
                    DimensionValor = row.DimensionValue,
                    ValorDecimal = row.Value,
                    CreadoEn = DateTime.UtcNow
                });
                changed++;
            }
            else if (existing.ValorDecimal != row.Value)
            {
                existing.ValorDecimal = row.Value;
                changed++;
            }
        }
        await SaveGuard(ct);
        return changed;
    }

    public Task<DashboardMetricasComercialesDto> Commercial(CancellationToken ct) => dashboard.CommercialMetrics(ct);
    public Task<DashboardReservasDto> Reservations(CancellationToken ct) => dashboard.Reservations(ct);
    public Task<DashboardPagosDto> Payments(CancellationToken ct) => dashboard.Payments(ct);
    public Task<DashboardOperacionDto> Operation(CancellationToken ct) => dashboard.Operation(ct);

    public async Task<ReporteClientesDto> Clients(CancellationToken ct)
    {
        var active = await StateId("CLIENTE", "ACTIVO", ct);
        return new(await db.Clientes.AsNoTracking().CountAsync(ct),
            await db.Clientes.AsNoTracking().CountAsync(x => x.EstadoId == active, ct),
            await ClientsByMonth(ct), await ClientsByState(ct));
    }

    public async Task<DashboardSerieDto[]> Reviews(CancellationToken ct) =>
        await (from r in db.Resenas.AsNoTracking()
            join d in db.Destinos.AsNoTracking() on r.DestinoId equals d.DestinoId
            group r by d.Nombre into g
            orderby g.Average(x => x.Calificacion) descending
            select new DashboardSerieDto(g.Key, (decimal)g.Average(x => x.Calificacion))).Take(10).ToArrayAsync(ct);

    public async Task<EventoAnaliticaDto> CreateEvent(long? userId, string? sessionId, string? ip,
        string? userAgent, EventoAnaliticaRequest request, CancellationToken ct)
    {
        var clientId = userId is null ? null : await db.Clientes.AsNoTracking()
            .Where(x => x.UsuarioId == userId.Value).Select(x => (long?)x.ClienteId).SingleOrDefaultAsync(ct);
        var item = new EventoAnalitica
        {
            UsuarioId = userId,
            ClienteId = clientId,
            SesionId = sessionId,
            TipoEvento = request.TipoEvento,
            Pagina = request.Pagina,
            RecursoId = request.RecursoId,
            DatosJson = request.Datos is null ? null : JsonSerializer.Serialize(request.Datos.Value),
            Ip = ip,
            UserAgent = userAgent,
            CreadoEn = DateTime.UtcNow
        };
        db.EventosAnalitica.Add(item);
        await SaveGuard(ct);
        return await EventQuery().SingleAsync(x => x.EventoAnaliticaId == Id(item.EventoAnaliticaId), ct);
    }

    public async Task<EventoAnaliticaPage> Events(string? type, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct)
    {
        var query = EventQuery()
            .Where(x => type == null || x.TipoEvento == type)
            .Where(x => from == null || x.CreadoEn >= from)
            .Where(x => to == null || x.CreadoEn <= to);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).Skip((page - 1) * pageSize)
            .Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<EventoResumenDto[]> EventSummary(CancellationToken ct) =>
        await db.EventosAnalitica.AsNoTracking().GroupBy(x => x.TipoEvento)
            .Select(g => new EventoResumenDto(g.Key, g.Count(), g.Max(x => (DateTime?)x.CreadoEn)))
            .OrderByDescending(x => x.Total).Take(20).ToArrayAsync(ct);

    public async Task<DashboardSnapshotPage> Snapshots(string? dashboardCode, int page, int pageSize, CancellationToken ct)
    {
        var query = SnapshotQuery().Where(x => dashboardCode == null || x.CodigoDashboard == dashboardCode);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.GeneradoEn).Skip((page - 1) * pageSize)
            .Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public Task<DashboardSnapshotDto?> Snapshot(long id, CancellationToken ct) =>
        SnapshotQuery().SingleOrDefaultAsync(x => x.DashboardSnapshotId == Id(id), ct);

    public async Task<DashboardSnapshotDto> CreateSnapshot(long userId, DashboardSnapshotRequest request, CancellationToken ct)
    {
        var item = new DashboardSnapshot
        {
            CodigoDashboard = request.CodigoDashboard,
            Titulo = request.Titulo,
            DatosJson = JsonSerializer.Serialize(request.Datos),
            GeneradoPorUsuarioId = userId,
            GeneradoEn = DateTime.UtcNow
        };
        db.DashboardSnapshots.Add(item);
        await SaveGuard(ct);
        return await Snapshot(item.DashboardSnapshotId, ct)
            ?? throw new SecurityFault(500, "No se pudo leer el snapshot generado.");
    }

    private IQueryable<EventoAnaliticaDto> EventQuery() =>
        db.EventosAnalitica.AsNoTracking().Select(x => new EventoAnaliticaDto(Id(x.EventoAnaliticaId),
            x.UsuarioId == null ? null : Id(x.UsuarioId.Value), x.ClienteId == null ? null : Id(x.ClienteId.Value),
            x.SesionId, x.TipoEvento, x.Pagina, x.RecursoId, x.DatosJson, x.Ip, x.UserAgent, x.CreadoEn));

    private IQueryable<DashboardSnapshotDto> SnapshotQuery() =>
        db.DashboardSnapshots.AsNoTracking().Select(x => new DashboardSnapshotDto(Id(x.DashboardSnapshotId),
            x.CodigoDashboard, x.Titulo, x.DatosJson,
            x.GeneradoPorUsuarioId == null ? null : Id(x.GeneradoPorUsuarioId.Value), x.GeneradoEn));

    private async Task<DashboardSerieDto[]> ClientsByState(CancellationToken ct) =>
        await (from c in db.Clientes.AsNoTracking()
            join e in db.Estados.AsNoTracking() on c.EstadoId equals e.EstadoId
            group c by e.Codigo into g
            select new DashboardSerieDto(g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> ClientsByMonth(CancellationToken ct)
    {
        var rows = await db.Clientes.AsNoTracking().GroupBy(x => new { x.CreadoEn.Year, x.CreadoEn.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).Take(12).ToArrayAsync(ct);
        return rows.Select(x => new DashboardSerieDto($"{x.Year}-{x.Month:00}", x.Total)).ToArray();
    }

    private async Task<decimal> SalesForDay(DateOnly date, CancellationToken ct) =>
        await db.Compras.AsNoTracking().Where(x => x.FechaCompra >= Start(date) && x.FechaCompra < End(date))
            .SumAsync(x => (decimal?)x.TotalPagado, ct) ?? 0;

    private async Task<decimal> CountComprasForDay(DateOnly date, CancellationToken ct) =>
        await db.Compras.AsNoTracking().CountAsync(x => x.FechaCompra >= Start(date) && x.FechaCompra < End(date), ct);

    private async Task<decimal> CountReservasForDay(DateOnly date, CancellationToken ct) =>
        await db.Reservas.AsNoTracking().CountAsync(x => x.FechaReserva >= Start(date) && x.FechaReserva < End(date), ct);

    private async Task<decimal> CountClientesForDay(DateOnly date, CancellationToken ct) =>
        await db.Clientes.AsNoTracking().CountAsync(x => x.CreadoEn >= Start(date) && x.CreadoEn < End(date), ct);

    private async Task<decimal> AvailableCapacity(CancellationToken ct) =>
        await db.CuposSalida.AsNoTracking().SumAsync(x => (decimal?)(x.CupoTotal - x.CupoReservado), ct) ?? 0;

    private async Task<decimal> PendingReviews(CancellationToken ct)
    {
        var pending = await StateId("RESENA", "PENDIENTE", ct);
        return await db.Resenas.AsNoTracking().CountAsync(x => x.EstadoModeracionId == pending, ct);
    }

    private async Task<MetricaSeed[]> ReservationsByStateForDay(DateOnly date, CancellationToken ct) =>
        await (from r in db.Reservas.AsNoTracking()
            join e in db.Estados.AsNoTracking() on r.EstadoId equals e.EstadoId
            where r.FechaReserva >= Start(date) && r.FechaReserva < End(date)
            group r by e.Codigo into g
            select new MetricaSeed("RESERVAS_CREADAS_DIA", "ESTADO_RESERVA", g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<MetricaSeed[]> PaymentsByStateForDay(DateOnly date, CancellationToken ct) =>
        await (from p in db.Pagos.AsNoTracking()
            join e in db.Estados.AsNoTracking() on p.EstadoId equals e.EstadoId
            where p.CreadoEn >= Start(date) && p.CreadoEn < End(date)
            group p by e.Codigo into g
            select new MetricaSeed("PAGOS_DIA", "ESTADO_PAGO", g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<MetricaSeed[]> TopDestinationsForDay(DateOnly date, CancellationToken ct) =>
        await (from c in db.Compras.AsNoTracking()
            join r in db.Reservas.AsNoTracking() on c.ReservaId equals r.ReservaId
            join s in db.Salidas.AsNoTracking() on r.SalidaId equals s.SalidaId
            join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
            join d in db.Destinos.AsNoTracking() on p.DestinoId equals d.DestinoId
            where c.FechaCompra >= Start(date) && c.FechaCompra < End(date)
            group c by d.Nombre into g
            select new MetricaSeed("DESTINOS_MAS_RESERVADOS", "DESTINO", g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<int> StateId(string group, string code, CancellationToken ct) =>
        await db.Estados.AsNoTracking().Where(x => x.Grupo == group && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado {group}.{code} requerido en WayPathDB.");

    private async Task SaveGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo guardar el reporte porque no cumple las restricciones de WayPathDB."); }
    }

    private static DateTime Start(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);
    private static DateTime End(DateOnly date) => date.AddDays(1).ToDateTime(TimeOnly.MinValue);

    private sealed record MetricaSeed(string Name, string? DimensionName, string? DimensionValue, decimal Value)
    {
        public string? KpiCode => Name switch
        {
            "VENTAS_TOTAL_DIA" or "COMPRAS_EMITIDAS_DIA" => "VENTAS_MENSUALES",
            "DESTINOS_MAS_RESERVADOS" => "DESTINOS_MAS_RESERVADOS",
            "PAGOS_DIA" => "METODOS_PAGO",
            _ => null
        };
    }
}
