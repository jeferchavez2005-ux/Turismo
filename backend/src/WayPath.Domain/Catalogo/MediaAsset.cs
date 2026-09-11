namespace WayPath.Domain.Catalogo;

public sealed class MediaAsset
{
    public long MediaAssetId { get; set; }
    public string Tipo { get; set; } = "";
    public string Url { get; set; } = "";
    public string? NombreArchivo { get; set; }
    public string? MimeType { get; set; }
    public string? TextoAlternativo { get; set; }
    public bool EsActivo { get; set; }
    public DateTime CreadoEn { get; set; }
}
