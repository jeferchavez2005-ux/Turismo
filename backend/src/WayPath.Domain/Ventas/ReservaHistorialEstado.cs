namespace WayPath.Domain.Ventas;

public sealed class ReservaHistorialEstado
{
    public long ReservaHistorialEstadoId { get; set; }
    public long ReservaId { get; set; }
    public int? EstadoAnteriorId { get; set; }
    public int EstadoNuevoId { get; set; }
    public long? UsuarioId { get; set; }
    public string? Comentario { get; set; }
    public DateTime CreadoEn { get; set; }
}
