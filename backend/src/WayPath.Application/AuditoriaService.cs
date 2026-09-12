using System.Text.Json;

namespace WayPath.Application;

public sealed class AuditoriaService(IAuditoriaStore store)
{
    private static readonly HashSet<string> AccessResults = ["EXITOSO", "FALLIDO", "BLOQUEADO", "LOGOUT", "REFRESH"];
    private static readonly HashSet<string> ErrorLevels = ["INFO", "WARN", "ERROR", "CRITICAL"];
    private static readonly HashSet<string> Operations = ["INSERT", "UPDATE", "DELETE"];

    public Task<AuditoriaCambioPage> Changes(long? userId, string? schema, string? table, string? operation,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct) =>
        store.Changes(userId, Upper(schema), Optional(table), Upper(operation), from, to, page, pageSize, ct);
    public Task<AuditoriaCambioDto?> Change(long id, CancellationToken ct) => store.Change(id, ct);
    public Task<AuditoriaAccesoPage> Accesses(long? userId, string? result, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct) =>
        store.Accesses(userId, Upper(result), from, to, page, pageSize, ct);
    public Task<ErrorAplicacionPage> Errors(string? level, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct) => store.Errors(Upper(level), from, to, page, pageSize, ct);
    public Task<ErrorAplicacionDto?> Error(long id, CancellationToken ct) => store.Error(id, ct);
    public Task<AuditoriaResumenDto> Summary(CancellationToken ct) => store.Summary(ct);
    public Task<ActividadRecienteDto> RecentActivity(CancellationToken ct) => store.RecentActivity(ct);

    public Task RegisterAccess(long? userId, string? email, string result, string? ip, string? userAgent, CancellationToken ct)
    {
        var clean = RequireAllowed(result, AccessResults, "Resultado de acceso invalido.");
        return store.RegisterAccess(userId, Optional(email), clean, Optional(ip), Optional(userAgent), ct);
    }

    public Task RegisterChange(long? userId, string schema, string table, string key, string operation,
        JsonElement? before, JsonElement? after, string? ip, CancellationToken ct) =>
        store.RegisterChange(userId, Required(schema), Required(table), Required(key),
            RequireAllowed(operation, Operations, "Operacion de auditoria invalida."), before, after, Optional(ip), ct);

    public Task RegisterError(long? userId, string level, string origin, string message,
        string? stackTrace, JsonElement? data, CancellationToken ct) =>
        store.RegisterError(userId, RequireAllowed(level, ErrorLevels, "Nivel de error invalido."),
            Required(origin), Required(message), Optional(stackTrace), data, ct);

    private static string RequireAllowed(string value, HashSet<string> allowed, string error)
    {
        var clean = Required(value).ToUpperInvariant();
        if (!allowed.Contains(clean)) throw new SecurityFault(400, error);
        return clean;
    }

    private static string Required(string value, string error = "El valor es obligatorio.")
    {
        var text = value.Trim();
        if (text.Length == 0) throw new SecurityFault(400, error);
        return text;
    }

    private static string? Optional(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text[..Math.Min(text.Length, 500)];
    }

    private static string? Upper(string? value) => Optional(value)?.ToUpperInvariant();
}
