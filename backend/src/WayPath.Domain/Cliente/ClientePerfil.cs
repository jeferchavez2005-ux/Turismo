namespace WayPath.Domain.Cliente;

public sealed class ClientePerfil
{
    public long ClienteId { get; set; }
    public long UsuarioId { get; set; }
    public int EstadoId { get; set; }
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Telefono { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public byte[] FilaVersion { get; set; } = [];
}
