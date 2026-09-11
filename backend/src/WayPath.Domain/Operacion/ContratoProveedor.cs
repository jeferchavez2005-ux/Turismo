namespace WayPath.Domain.Operacion;

public sealed class ContratoProveedor
{
    public long ContratoProveedorId { get; set; }
    public long ProveedorId { get; set; }
    public string CodigoContrato { get; set; } = "";
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public string? Condiciones { get; set; }
    public int EstadoId { get; set; }
}
