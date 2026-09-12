namespace WayPath.Domain.Reportes;

public sealed class EventoAnalitica
{
    public long EventoAnaliticaId { get; set; }
    public long? UsuarioId { get; set; }
    public long? ClienteId { get; set; }
    public string? SesionId { get; set; }
    public string TipoEvento { get; set; } = "";
    public string? Pagina { get; set; }
    public string? RecursoId { get; set; }
    public string? DatosJson { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreadoEn { get; set; }
}
