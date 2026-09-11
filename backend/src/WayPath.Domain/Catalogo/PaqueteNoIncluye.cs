namespace WayPath.Domain.Catalogo;

public sealed class PaqueteNoIncluye
{
    public long PaqueteNoIncluyeId { get; set; }
    public long PaqueteId { get; set; }
    public string Descripcion { get; set; } = "";
    public int Orden { get; set; }
}
