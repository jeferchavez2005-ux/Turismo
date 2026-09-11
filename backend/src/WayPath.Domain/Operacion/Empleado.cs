namespace WayPath.Domain.Operacion;

public sealed class Empleado
{
    public long EmpleadoId { get; set; }
    public long UsuarioId { get; set; }
    public int AreaId { get; set; }
    public string CodigoEmpleado { get; set; } = "";
    public string Cargo { get; set; } = "";
    public int EstadoId { get; set; }
    public DateOnly FechaIngreso { get; set; }
    public DateOnly? FechaSalida { get; set; }
}
