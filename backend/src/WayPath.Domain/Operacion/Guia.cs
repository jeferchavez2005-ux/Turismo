namespace WayPath.Domain.Operacion;

public sealed class Guia
{
    public long GuiaId { get; set; }
    public long? ProveedorId { get; set; }
    public int? TipoDocumentoId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Idiomas { get; set; }
    public int EstadoId { get; set; }
}
