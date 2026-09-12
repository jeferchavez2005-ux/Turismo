namespace WayPath.Domain.Auditoria;

public sealed class AuditoriaCambio
{
    public long AuditoriaCambioId { get; set; }
    public long? UsuarioId { get; set; }
    public string EsquemaNombre { get; set; } = "";
    public string TablaNombre { get; set; } = "";
    public string ClavePrimaria { get; set; } = "";
    public string Operacion { get; set; } = "";
    public string? DatosAntes { get; set; }
    public string? DatosDespues { get; set; }
    public string? Ip { get; set; }
    public DateTime CreadoEn { get; set; }
}
