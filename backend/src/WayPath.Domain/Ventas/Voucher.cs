namespace WayPath.Domain.Ventas;

public sealed class Voucher
{
    public long VoucherId { get; set; }
    public long CompraId { get; set; }
    public long ReservaId { get; set; }
    public int EstadoId { get; set; }
    public string CodigoVoucher { get; set; } = "";
    public string? UrlDocumento { get; set; }
    public DateTime EmitidoEn { get; set; }
}
