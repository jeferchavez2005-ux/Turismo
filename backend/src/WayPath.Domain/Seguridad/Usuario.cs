namespace WayPath.Domain.Seguridad;

public sealed class Usuario
{
    public long UsuarioId { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? NombreUsuario { get; set; }
    public int EstadoId { get; set; }
    public bool EmailConfirmado { get; set; }
    public bool TelefonoConfirmado { get; set; }
    public DateTime? UltimoAccesoEn { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public byte[] FilaVersion { get; set; } = [];
}

