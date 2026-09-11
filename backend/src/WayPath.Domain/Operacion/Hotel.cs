namespace WayPath.Domain.Operacion;

public sealed class Hotel
{
    public long HotelId { get; set; }
    public long? ProveedorId { get; set; }
    public int CiudadId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? CategoriaEstrellas { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int EstadoId { get; set; }
}
