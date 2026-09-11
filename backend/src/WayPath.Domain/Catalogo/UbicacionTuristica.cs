namespace WayPath.Domain.Catalogo;

public sealed class UbicacionTuristica
{
    public long UbicacionTuristicaId { get; set; }
    public int CiudadId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Direccion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}
