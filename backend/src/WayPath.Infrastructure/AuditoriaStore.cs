using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Auditoria;

namespace WayPath.Infrastructure;

public sealed class AuditoriaStore(WayPathDbContext db) : IAuditoriaStore
{
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<AuditoriaCambioPage> Changes(long? userId, string? schema, string? table, string? operation,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var query = ChangeQuery()
            .Where(x => userId == null || x.UsuarioId == Id(userId.Value))
            .Where(x => schema == null || x.EsquemaNombre == schema)
            .Where(x => table == null || x.TablaNombre == table)
            .Where(x => operation == null || x.Operacion == operation)
            .Where(x => from == null || x.CreadoEn >= from)
            .Where(x => to == null || x.CreadoEn <= to);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).Skip((page - 1) * pageSize)
            .Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public Task<AuditoriaCambioDto?> Change(long id, CancellationToken ct) =>
        ChangeQuery().SingleOrDefaultAsync(x => x.AuditoriaCambioId == Id(id), ct);

    public async Task<AuditoriaAccesoPage> Accesses(long? userId, string? result, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct)
    {
        var query = AccessQuery()
            .Where(x => userId == null || x.UsuarioId == Id(userId.Value))
            .Where(x => result == null || x.Resultado == result)
            .Where(x => from == null || x.CreadoEn >= from)
            .Where(x => to == null || x.CreadoEn <= to);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).Skip((page - 1) * pageSize)
            .Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<ErrorAplicacionPage> Errors(string? level, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct)
    {
        var query = ErrorQuery()
            .Where(x => level == null || x.Nivel == level)
            .Where(x => from == null || x.CreadoEn >= from)
            .Where(x => to == null || x.CreadoEn <= to);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).Skip((page - 1) * pageSize)
            .Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public Task<ErrorAplicacionDto?> Error(long id, CancellationToken ct) =>
        ErrorQuery().SingleOrDefaultAsync(x => x.ErrorAplicacionId == Id(id), ct);

    public async Task<AuditoriaResumenDto> Summary(CancellationToken ct)
    {
        var changesByTable = await db.AuditoriaCambios.AsNoTracking()
            .GroupBy(x => x.EsquemaNombre + "." + x.TablaNombre)
            .Select(g => new DashboardSerieDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Valor).Take(10).ToArrayAsync(ct);
        var errorsByLevel = await db.ErroresAplicacion.AsNoTracking()
            .GroupBy(x => x.Nivel)
            .Select(g => new DashboardSerieDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Valor).ToArrayAsync(ct);
        return new(
            await db.AuditoriaCambios.AsNoTracking().CountAsync(ct),
            await db.AuditoriaAccesos.AsNoTracking().CountAsync(x => x.Resultado == "EXITOSO", ct),
            await db.AuditoriaAccesos.AsNoTracking().CountAsync(x => x.Resultado == "FALLIDO" || x.Resultado == "BLOQUEADO", ct),
            await db.ErroresAplicacion.AsNoTracking().CountAsync(ct),
            await db.ErroresAplicacion.AsNoTracking().CountAsync(x => x.Nivel == "CRITICAL", ct),
            changesByTable, errorsByLevel);
    }

    public async Task<ActividadRecienteDto> RecentActivity(CancellationToken ct) =>
        new(await ChangeQuery().OrderByDescending(x => x.CreadoEn).Take(10).ToArrayAsync(ct),
            await AccessQuery().OrderByDescending(x => x.CreadoEn).Take(10).ToArrayAsync(ct),
            await ErrorQuery().OrderByDescending(x => x.CreadoEn).Take(10).ToArrayAsync(ct));

    public async Task RegisterAccess(long? userId, string? email, string result, string? ip, string? userAgent, CancellationToken ct)
    {
        db.AuditoriaAccesos.Add(new AuditoriaAcceso
        {
            UsuarioId = userId,
            EmailIntentado = email,
            Resultado = result,
            Ip = Trim(ip, 45),
            UserAgent = Trim(userAgent, 500),
            CreadoEn = DateTime.UtcNow
        });
        await SaveGuard(ct);
    }

    public async Task RegisterChange(long? userId, string schema, string table, string key, string operation,
        JsonElement? before, JsonElement? after, string? ip, CancellationToken ct)
    {
        db.AuditoriaCambios.Add(new AuditoriaCambio
        {
            UsuarioId = userId,
            EsquemaNombre = Trim(schema, 80)!,
            TablaNombre = Trim(table, 120)!,
            ClavePrimaria = Trim(key, 120)!,
            Operacion = operation,
            DatosAntes = before is null ? null : JsonSerializer.Serialize(before.Value),
            DatosDespues = after is null ? null : JsonSerializer.Serialize(after.Value),
            Ip = Trim(ip, 45),
            CreadoEn = DateTime.UtcNow
        });
        await SaveGuard(ct);
    }

    public async Task RegisterError(long? userId, string level, string origin, string message,
        string? stackTrace, JsonElement? data, CancellationToken ct)
    {
        db.ErroresAplicacion.Add(new ErrorAplicacion
        {
            UsuarioId = userId,
            Nivel = level,
            Origen = Trim(origin, 160)!,
            Mensaje = Trim(message, 4000)!,
            StackTrace = stackTrace,
            DatosJson = data is null ? null : JsonSerializer.Serialize(data.Value),
            CreadoEn = DateTime.UtcNow
        });
        await SaveGuard(ct);
    }

    private IQueryable<AuditoriaCambioDto> ChangeQuery() =>
        db.AuditoriaCambios.AsNoTracking().Select(x => new AuditoriaCambioDto(Id(x.AuditoriaCambioId),
            x.UsuarioId == null ? null : Id(x.UsuarioId.Value), x.EsquemaNombre, x.TablaNombre, x.ClavePrimaria,
            x.Operacion, x.DatosAntes, x.DatosDespues, x.Ip, x.CreadoEn));

    private IQueryable<AuditoriaAccesoDto> AccessQuery() =>
        db.AuditoriaAccesos.AsNoTracking().Select(x => new AuditoriaAccesoDto(Id(x.AuditoriaAccesoId),
            x.UsuarioId == null ? null : Id(x.UsuarioId.Value), x.EmailIntentado, x.Resultado,
            x.Ip, x.UserAgent, x.CreadoEn));

    private IQueryable<ErrorAplicacionDto> ErrorQuery() =>
        db.ErroresAplicacion.AsNoTracking().Select(x => new ErrorAplicacionDto(Id(x.ErrorAplicacionId),
            x.UsuarioId == null ? null : Id(x.UsuarioId.Value), x.Nivel, x.Origen, x.Mensaje,
            x.StackTrace, x.DatosJson, x.CreadoEn));

    private async Task SaveGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo registrar la auditoria."); }
    }

    private static string? Trim(string? value, int max)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text[..Math.Min(text.Length, max)];
    }
}
