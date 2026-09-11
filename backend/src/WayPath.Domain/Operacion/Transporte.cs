namespace WayPath.Domain.Operacion;

public sealed class Transporte
{
    public long TransporteId { get; set; }
    public long? ProveedorId { get; set; }
    public string TipoTransporte { get; set; } = "";
    public string? Placa { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int Capacidad { get; set; }
    public int EstadoId { get; set; }
}
