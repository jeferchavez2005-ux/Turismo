using Microsoft.EntityFrameworkCore;
using WayPath.Application;

namespace WayPath.Infrastructure;

public sealed class DashboardStore(WayPathDbContext db) : IDashboardStore
{
    public async Task<DashboardResumenDto> Summary(CancellationToken ct)
    {
        var activeClientState = await StateId("CLIENTE", "ACTIVO", ct);
        var approvedPaymentState = await StateId("PAGO", "APROBADO", ct);
        var pendingReviewState = await StateId("RESENA", "PENDIENTE", ct);

        var cupos = await db.CuposSalida.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(x => x.CupoTotal), Reservado = g.Sum(x => x.CupoReservado) })
            .SingleOrDefaultAsync(ct);

        return new(
            await db.Usuarios.AsNoTracking().CountAsync(ct),
            await db.Clientes.AsNoTracking().CountAsync(x => x.EstadoId == activeClientState, ct),
            await db.Reservas.AsNoTracking().CountAsync(ct),
            await db.Pagos.AsNoTracking().CountAsync(x => x.EstadoId == approvedPaymentState, ct),
            await db.Compras.AsNoTracking().SumAsync(x => (decimal?)x.TotalPagado, ct) ?? 0,
            await db.Compras.AsNoTracking().CountAsync(ct),
            Math.Max(0, (cupos?.Total ?? 0) - (cupos?.Reservado ?? 0)),
            await db.Resenas.AsNoTracking().CountAsync(x => x.EstadoModeracionId == pendingReviewState, ct));
    }

    public async Task<DashboardMetricasComercialesDto> CommercialMetrics(CancellationToken ct) =>
        new(await MonthlyIncome(ct), await TopDestinations(ct), await TopPackages(ct), await PaymentMethods(ct));

    public async Task<DashboardReservasDto> Reservations(CancellationToken ct) =>
        new(await db.Reservas.AsNoTracking().CountAsync(ct), await ReservationsByState(ct), await ReservationsByMonth(ct));

    public async Task<DashboardPagosDto> Payments(CancellationToken ct) =>
        new(await db.Pagos.AsNoTracking().CountAsync(ct), await PaymentsByState(ct), await PaymentMethods(ct));

    public async Task<DashboardVentasDto> Sales(CancellationToken ct) =>
        new(await db.Compras.AsNoTracking().SumAsync(x => (decimal?)x.TotalPagado, ct) ?? 0,
            await db.Compras.AsNoTracking().CountAsync(ct), await MonthlyIncome(ct),
            await TopDestinations(ct), await TopPackages(ct));

    public async Task<DashboardOperacionDto> Operation(CancellationToken ct)
    {
        var cupos = await db.CuposSalida.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(x => x.CupoTotal), Reservado = g.Sum(x => x.CupoReservado) })
            .SingleOrDefaultAsync(ct);
        var total = cupos?.Total ?? 0;
        var reserved = cupos?.Reservado ?? 0;
        return new(total, reserved, Math.Max(0, total - reserved),
            await CapacityByDeparture(ct), await DeparturesByState(ct));
    }

    private async Task<DashboardSerieDto[]> ReservationsByState(CancellationToken ct) =>
        await (from r in db.Reservas.AsNoTracking()
            join e in db.Estados.AsNoTracking() on r.EstadoId equals e.EstadoId
            group r by e.Codigo into g
            orderby g.Count() descending
            select new DashboardSerieDto(g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> PaymentsByState(CancellationToken ct) =>
        await (from p in db.Pagos.AsNoTracking()
            join e in db.Estados.AsNoTracking() on p.EstadoId equals e.EstadoId
            group p by e.Codigo into g
            orderby g.Count() descending
            select new DashboardSerieDto(g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> PaymentMethods(CancellationToken ct) =>
        await (from p in db.Pagos.AsNoTracking()
            join m in db.MetodosPago.AsNoTracking() on p.MetodoPagoId equals m.MetodoPagoId
            group p by m.Nombre into g
            orderby g.Count() descending
            select new DashboardSerieDto(g.Key, g.Count())).Take(6).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> TopDestinations(CancellationToken ct) =>
        await (from c in db.Compras.AsNoTracking()
            join r in db.Reservas.AsNoTracking() on c.ReservaId equals r.ReservaId
            join s in db.Salidas.AsNoTracking() on r.SalidaId equals s.SalidaId
            join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
            join d in db.Destinos.AsNoTracking() on p.DestinoId equals d.DestinoId
            group c by d.Nombre into g
            orderby g.Count() descending
            select new DashboardSerieDto(g.Key, g.Count())).Take(8).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> TopPackages(CancellationToken ct) =>
        await (from c in db.Compras.AsNoTracking()
            join r in db.Reservas.AsNoTracking() on c.ReservaId equals r.ReservaId
            join s in db.Salidas.AsNoTracking() on r.SalidaId equals s.SalidaId
            join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
            group c by p.Nombre into g
            orderby g.Count() descending
            select new DashboardSerieDto(g.Key, g.Count())).Take(8).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> MonthlyIncome(CancellationToken ct)
    {
        var rows = await db.Compras.AsNoTracking()
            .GroupBy(x => new { x.FechaCompra.Year, x.FechaCompra.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.TotalPagado) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Take(12)
            .ToArrayAsync(ct);
        return rows.Select(x => new DashboardSerieDto($"{x.Year}-{x.Month:00}", x.Total)).ToArray();
    }

    private async Task<DashboardSerieDto[]> ReservationsByMonth(CancellationToken ct)
    {
        var rows = await db.Reservas.AsNoTracking()
            .GroupBy(x => new { x.FechaReserva.Year, x.FechaReserva.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Take(12)
            .ToArrayAsync(ct);
        return rows.Select(x => new DashboardSerieDto($"{x.Year}-{x.Month:00}", x.Total)).ToArray();
    }

    private async Task<DashboardSerieDto[]> CapacityByDeparture(CancellationToken ct) =>
        await (from c in db.CuposSalida.AsNoTracking()
            join s in db.Salidas.AsNoTracking() on c.SalidaId equals s.SalidaId
            join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
            group c by new { c.SalidaId, p.Nombre, s.FechaInicio } into g
            orderby g.Key.FechaInicio
            select new DashboardSerieDto(g.Key.Nombre, g.Sum(x => x.CupoTotal - x.CupoReservado)))
            .Take(10).ToArrayAsync(ct);

    private async Task<DashboardSerieDto[]> DeparturesByState(CancellationToken ct) =>
        await (from s in db.Salidas.AsNoTracking()
            join e in db.Estados.AsNoTracking() on s.EstadoId equals e.EstadoId
            group s by e.Codigo into g
            orderby g.Count() descending
            select new DashboardSerieDto(g.Key, g.Count())).ToArrayAsync(ct);

    private async Task<int> StateId(string group, string code, CancellationToken ct) =>
        await db.Estados.AsNoTracking().Where(x => x.Grupo == group && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado {group}.{code} requerido en WayPathDB.");
}
