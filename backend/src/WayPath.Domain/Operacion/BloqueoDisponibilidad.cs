namespace WayPath.Domain.Operacion;

public sealed class BloqueoDisponibilidad
{
    public long BloqueoDisponibilidadId { get; set; }
    public long? PaqueteId { get; set; }
    public long? SalidaId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string Motivo { get; set; } = "";
    public long? CreadoPorUsuarioId { get; set; }
    public DateTime CreadoEn { get; set; }
}
