namespace WayPath.Domain.Seguridad;

public sealed class UsuarioRol
{
    public long UsuarioId { get; set; }
    public int RolId { get; set; }
    public DateTime AsignadoEn { get; set; }
    public long? AsignadoPorUsuarioId { get; set; }
}

