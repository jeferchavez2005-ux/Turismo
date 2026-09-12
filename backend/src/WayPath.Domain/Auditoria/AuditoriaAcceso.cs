namespace WayPath.Domain.Auditoria;

public sealed class AuditoriaAcceso
{
    public long AuditoriaAccesoId { get; set; }
    public long? UsuarioId { get; set; }
    public string? EmailIntentado { get; set; }
    public string Resultado { get; set; } = "";
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreadoEn { get; set; }
}
