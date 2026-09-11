namespace WayPath.Domain.Operacion;

public sealed class AsignacionOperacion
{
    public long AsignacionOperacionId { get; set; }
    public long SalidaId { get; set; }
    public long? ProveedorServicioId { get; set; }
    public long? GuiaId { get; set; }
    public long? TransporteId { get; set; }
    public long? HotelId { get; set; }
    public long? HabitacionId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public string? Observaciones { get; set; }
}
