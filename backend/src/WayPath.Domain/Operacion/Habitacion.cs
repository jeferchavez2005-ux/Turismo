namespace WayPath.Domain.Operacion;

public sealed class Habitacion
{
    public long HabitacionId { get; set; }
    public long HotelId { get; set; }
    public int MonedaId { get; set; }
    public string TipoHabitacion { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal PrecioNoche { get; set; }
    public int EstadoId { get; set; }
}
