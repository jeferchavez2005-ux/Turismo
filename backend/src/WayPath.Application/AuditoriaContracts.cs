using System.Text.Json;

namespace WayPath.Application;

public sealed record AuditoriaCambioDto(string AuditoriaCambioId, string? UsuarioId, string EsquemaNombre,
    string TablaNombre, string ClavePrimaria, string Operacion, string? DatosAntes,
    string? DatosDespues, string? Ip, DateTime CreadoEn);

public sealed record AuditoriaAccesoDto(string AuditoriaAccesoId, string? UsuarioId, string? EmailIntentado,
    string Resultado, string? Ip, string? UserAgent, DateTime CreadoEn);

public sealed record ErrorAplicacionDto(string ErrorAplicacionId, string? UsuarioId, string Nivel,
    string Origen, string Mensaje, string? StackTrace, string? DatosJson, DateTime CreadoEn);

public sealed record AuditoriaResumenDto(int Cambios, int AccesosExitosos, int AccesosFallidos,
    int Errores, int Criticos, DashboardSerieDto[] CambiosPorTabla, DashboardSerieDto[] ErroresPorNivel);

public sealed record ActividadRecienteDto(AuditoriaCambioDto[] Cambios, AuditoriaAccesoDto[] Accesos,
    ErrorAplicacionDto[] Errores);

public sealed record AuditoriaCambioPage(AuditoriaCambioDto[] Items, int Page, int PageSize, int Total);
public sealed record AuditoriaAccesoPage(AuditoriaAccesoDto[] Items, int Page, int PageSize, int Total);
public sealed record ErrorAplicacionPage(ErrorAplicacionDto[] Items, int Page, int PageSize, int Total);

public interface IAuditoriaStore
{
    Task<AuditoriaCambioPage> Changes(long? userId, string? schema, string? table, string? operation,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<AuditoriaCambioDto?> Change(long id, CancellationToken ct);
    Task<AuditoriaAccesoPage> Accesses(long? userId, string? result, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct);
    Task<ErrorAplicacionPage> Errors(string? level, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct);
    Task<ErrorAplicacionDto?> Error(long id, CancellationToken ct);
    Task<AuditoriaResumenDto> Summary(CancellationToken ct);
    Task<ActividadRecienteDto> RecentActivity(CancellationToken ct);
    Task RegisterAccess(long? userId, string? email, string result, string? ip, string? userAgent, CancellationToken ct);
    Task RegisterChange(long? userId, string schema, string table, string key, string operation,
        JsonElement? before, JsonElement? after, string? ip, CancellationToken ct);
    Task RegisterError(long? userId, string level, string origin, string message,
        string? stackTrace, JsonElement? data, CancellationToken ct);
}
