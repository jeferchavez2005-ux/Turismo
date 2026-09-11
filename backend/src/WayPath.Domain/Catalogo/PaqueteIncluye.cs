namespace WayPath.Domain.Catalogo;

public sealed class PaqueteIncluye
{
    public long PaqueteIncluyeId { get; set; }
    public long PaqueteId { get; set; }
    public string Descripcion { get; set; } = "";
    public int Orden { get; set; }
}
