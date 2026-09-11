namespace WayPath.Domain.Ventas;

public sealed class Resena
{
    public long ResenaId { get; set; }
    public long ClienteId { get; set; }
    public long? ReservaId { get; set; }
    public long DestinoId { get; set; }
    public int EstadoModeracionId { get; set; }
    public byte Calificacion { get; set; }
    public string? Titulo { get; set; }
    public string Comentario { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public DateTime? ModeradoEn { get; set; }
    public long? ModeradoPorUsuarioId { get; set; }
}
