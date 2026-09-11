namespace WayPath.Domain.Catalogo;

public sealed class DestinoImagen
{
    public long DestinoImagenId { get; set; }
    public long DestinoId { get; set; }
    public long MediaAssetId { get; set; }
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
}
