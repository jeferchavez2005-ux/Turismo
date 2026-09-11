namespace WayPath.Domain.Cliente;

public sealed class Pasajero
{
    public long PasajeroId { get; set; }
    public long? ClienteId { get; set; }
    public int? TipoDocumentoId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public int? NacionalidadPaisId { get; set; }
    public string? RequerimientosEspeciales { get; set; }
    public DateTime CreadoEn { get; set; }
}
