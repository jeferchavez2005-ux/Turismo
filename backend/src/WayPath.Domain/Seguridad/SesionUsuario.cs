namespace WayPath.Domain.Seguridad;

public sealed class SesionUsuario
{
    public long SesionUsuarioId { get; set; }
    public long UsuarioId { get; set; }
    public string IdentificadorSesion { get; set; } = "";
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime IniciadaEn { get; set; }
    public DateTime? FinalizadaEn { get; set; }
}

