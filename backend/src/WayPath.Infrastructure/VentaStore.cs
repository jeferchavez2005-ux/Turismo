using System.Data;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Ventas;

namespace WayPath.Infrastructure;

public sealed class VentaStore(WayPathDbContext db) : IVentaStore
{
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<CompraDetalleDto> EmitFromApprovedPayment(long pagoId, CancellationToken ct)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var payment = await db.Pagos.SingleOrDefaultAsync(x => x.PagoId == pagoId, ct)
            ?? throw new SecurityFault(404, "Pago no encontrado.");
        var paymentState = await StateCode(payment.EstadoId, ct);
        if (paymentState != "APROBADO")
            throw new SecurityFault(409, "Solo se puede emitir venta desde un pago aprobado.");

        var existing = await db.Compras.SingleOrDefaultAsync(x => x.ReservaId == payment.ReservaId, ct);
        if (existing is not null)
        {
            if (tx is not null) await tx.CommitAsync(ct);
            return await Detail(existing.CompraId, ct) ?? throw new SecurityFault(500, "No se pudo leer la compra existente.");
        }

        var reserva = await db.Reservas.SingleAsync(x => x.ReservaId == payment.ReservaId, ct);
        if (reserva.Total != payment.Monto || reserva.MonedaId != payment.MonedaId)
            throw new SecurityFault(409, "El pago aprobado no coincide con la reserva.");

        var compra = new Compra
        {
            ReservaId = reserva.ReservaId,
            ClienteId = reserva.ClienteId,
            PagoId = payment.PagoId,
            EstadoId = await StateId("COMPRA", "EMITIDA", ct),
            CodigoCompra = await NextCode("CP", exists => db.Compras.AnyAsync(x => x.CodigoCompra == exists, ct)),
            FechaCompra = DateTime.UtcNow,
            TotalPagado = payment.Monto
        };
        db.Compras.Add(compra);
        await SaveGuard(ct);

        var comprobante = new Comprobante
        {
            CompraId = compra.CompraId,
            ClienteId = compra.ClienteId,
            MonedaId = reserva.MonedaId,
            EstadoId = await StateId("COMPROBANTE", "EMITIDO", ct),
            TipoComprobante = "BOLETA",
            Serie = "B001",
            Numero = await NextReceiptNumber(ct),
            FechaEmision = DateTime.UtcNow,
            Subtotal = reserva.Subtotal,
            Impuestos = reserva.Impuestos,
            Total = reserva.Total
        };
        db.Comprobantes.Add(comprobante);
        await SaveGuard(ct);

        var reservationLines = await db.ReservaDetalle.AsNoTracking()
            .Where(x => x.ReservaId == reserva.ReservaId).ToArrayAsync(ct);
        db.ComprobanteDetalle.AddRange(reservationLines.Select(x => new ComprobanteDetalle
        {
            ComprobanteId = comprobante.ComprobanteId,
            Descripcion = x.Descripcion,
            Cantidad = x.Cantidad,
            PrecioUnitario = x.PrecioUnitario,
            Importe = x.Importe
        }));

        var ticketState = await StateId("TICKET_VIAJE", "EMITIDO", ct);
        var voucherState = await StateId("COMPROBANTE", "EMITIDO", ct);
        var passengers = await db.ReservaPasajeros.AsNoTracking()
            .Where(x => x.ReservaId == reserva.ReservaId).ToArrayAsync(ct);
        foreach (var passenger in passengers)
        {
            var code = await NextCode("TK", exists => db.TicketsViaje.AnyAsync(x => x.CodigoTicket == exists, ct));
            db.TicketsViaje.Add(new TicketViaje
            {
                CompraId = compra.CompraId,
                ReservaId = reserva.ReservaId,
                PasajeroId = passenger.PasajeroId,
                SalidaId = reserva.SalidaId,
                EstadoId = ticketState,
                CodigoTicket = code,
                EmitidoEn = DateTime.UtcNow,
                UrlDocumento = null
            });
        }

        var voucherCode = await NextCode("VC", exists => db.Vouchers.AnyAsync(x => x.CodigoVoucher == exists, ct));
        db.Vouchers.Add(new Voucher
        {
            CompraId = compra.CompraId,
            ReservaId = reserva.ReservaId,
            EstadoId = voucherState,
            CodigoVoucher = voucherCode,
            UrlDocumento = null,
            EmitidoEn = DateTime.UtcNow
        });
        await SaveGuard(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return await Detail(compra.CompraId, ct) ?? throw new SecurityFault(500, "No se pudo leer la compra emitida.");
    }

    public async Task<CompraResumenDto[]> Mine(long usuarioId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        return await SummaryQuery().Where(x => x.ClienteId == clienteId)
            .OrderByDescending(x => x.FechaCompra).Select(x => x.Dto).ToArrayAsync(ct);
    }

    public async Task<CompraDetalleDto?> DetailForUser(long usuarioId, long compraId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var belongs = await db.Compras.AsNoTracking().AnyAsync(x => x.CompraId == compraId && x.ClienteId == clienteId, ct);
        return belongs ? await Detail(compraId, ct) : null;
    }

    public Task<CompraDetalleDto?> DetailAdmin(long compraId, CancellationToken ct) => Detail(compraId, ct);

    public async Task<TicketViajeDto?> TicketForUser(long usuarioId, long ticketId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var belongs = await (from t in db.TicketsViaje.AsNoTracking()
            join c in db.Compras.AsNoTracking() on t.CompraId equals c.CompraId
            where t.TicketViajeId == ticketId && c.ClienteId == clienteId
            select t.TicketViajeId).AnyAsync(ct);
        return belongs ? await TicketQuery().SingleOrDefaultAsync(x => x.TicketViajeId == Id(ticketId), ct) : null;
    }

    public async Task<VoucherDto?> VoucherForUser(long usuarioId, long voucherId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var belongs = await (from v in db.Vouchers.AsNoTracking()
            join c in db.Compras.AsNoTracking() on v.CompraId equals c.CompraId
            where v.VoucherId == voucherId && c.ClienteId == clienteId
            select v.VoucherId).AnyAsync(ct);
        return belongs ? await VoucherQuery().SingleOrDefaultAsync(x => x.VoucherId == Id(voucherId), ct) : null;
    }

    public async Task<CompraPage> List(int page, int pageSize, CancellationToken ct)
    {
        var query = SummaryQuery();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.FechaCompra)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => x.Dto).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task SetCompraState(long compraId, string codigo, CancellationToken ct)
    {
        var compra = await db.Compras.SingleOrDefaultAsync(x => x.CompraId == compraId, ct)
            ?? throw new SecurityFault(404, "Compra no encontrada.");
        compra.EstadoId = await StateId("COMPRA", codigo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetComprobanteState(long comprobanteId, string codigo, CancellationToken ct)
    {
        var comprobante = await db.Comprobantes.SingleOrDefaultAsync(x => x.ComprobanteId == comprobanteId, ct)
            ?? throw new SecurityFault(404, "Comprobante no encontrado.");
        comprobante.EstadoId = await StateId("COMPROBANTE", codigo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ResenaDto[]> PublicReviews(long? destinoId, byte? calificacion, CancellationToken ct)
    {
        var approved = await StateId("RESENA", "APROBADA", ct);
        return await ReviewQuery().Where(x => x.EstadoModeracion == "APROBADA")
            .Where(x => destinoId == null || db.Resenas.Any(r => r.ResenaId.ToString() == x.ResenaId && r.DestinoId == destinoId))
            .Where(x => calificacion == null || x.Calificacion == calificacion)
            .OrderByDescending(x => x.CreadoEn).ToArrayAsync(ct);
    }

    public async Task<ResenaDto> CreateReview(long usuarioId, CrearResenaRequest request, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var reserva = await (from r in db.Reservas
            join s in db.Salidas on r.SalidaId equals s.SalidaId
            join p in db.Paquetes on s.PaqueteId equals p.PaqueteId
            where r.ReservaId == request.ReservaId && r.ClienteId == clienteId
            select new { Reserva = r, p.DestinoId }).SingleOrDefaultAsync(ct)
            ?? throw new SecurityFault(404, "Reserva no encontrada.");
        var state = await StateCode(reserva.Reserva.EstadoId, ct);
        if (state != "COMPLETADA")
            throw new SecurityFault(409, "Solo puede crear resenas de viajes completados.");
        if (await db.Resenas.AnyAsync(x => x.ClienteId == clienteId && x.ReservaId == request.ReservaId, ct))
            throw new SecurityFault(409, "Ya existe una resena para esta reserva.");
        var review = new Resena
        {
            ClienteId = clienteId,
            ReservaId = request.ReservaId,
            DestinoId = reserva.DestinoId,
            EstadoModeracionId = await StateId("RESENA", "PENDIENTE", ct),
            Calificacion = request.Calificacion,
            Titulo = request.Titulo,
            Comentario = request.Comentario,
            CreadoEn = DateTime.UtcNow
        };
        db.Resenas.Add(review);
        await SaveGuard(ct);
        return await ReviewQuery().SingleAsync(x => x.ResenaId == Id(review.ResenaId), ct);
    }

    public async Task<ResenaPage> ListReviews(int page, int pageSize, CancellationToken ct)
    {
        var query = ReviewQuery();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task ModerateReview(long resenaId, ModerarResenaRequest request, long actorId, CancellationToken ct)
    {
        var review = await db.Resenas.SingleOrDefaultAsync(x => x.ResenaId == resenaId, ct)
            ?? throw new SecurityFault(404, "Resena no encontrada.");
        review.EstadoModeracionId = await StateId("RESENA", request.Estado, ct);
        review.ModeradoPorUsuarioId = actorId;
        review.ModeradoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<CompraDetalleDto?> Detail(long compraId, CancellationToken ct)
    {
        var header = await SummaryQuery().SingleOrDefaultAsync(x => x.CompraId == compraId, ct);
        if (header is null) return null;
        var comprobante = await ComprobanteQuery().SingleOrDefaultAsync(x => x.CompraId == Id(compraId), ct);
        var tickets = await TicketQuery().Where(x => x.CompraId == Id(compraId)).ToArrayAsync(ct);
        var voucher = await VoucherQuery().SingleOrDefaultAsync(x => x.CompraId == Id(compraId), ct);
        return new(Id(header.CompraId), header.Dto.CodigoCompra, header.Dto.ReservaId,
            header.Dto.CodigoReserva, header.Dto.Cliente, header.Dto.Paquete, header.Dto.Destino,
            header.Dto.FechaInicio, header.Dto.FechaFin, header.Dto.Estado, header.Dto.FechaCompra,
            header.Dto.TotalPagado, comprobante, tickets, voucher);
    }

    private IQueryable<CompraRow> SummaryQuery() =>
        from c in db.Compras.AsNoTracking()
        join r in db.Reservas.AsNoTracking() on c.ReservaId equals r.ReservaId
        join cl in db.Clientes.AsNoTracking() on c.ClienteId equals cl.ClienteId
        join s in db.Salidas.AsNoTracking() on r.SalidaId equals s.SalidaId
        join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
        join d in db.Destinos.AsNoTracking() on p.DestinoId equals d.DestinoId
        join e in db.Estados.AsNoTracking() on c.EstadoId equals e.EstadoId
        select new CompraRow(c.CompraId, c.ClienteId, c.FechaCompra,
            new CompraResumenDto(Id(c.CompraId), c.CodigoCompra, Id(c.ReservaId),
                r.CodigoReserva, cl.Nombres + " " + cl.Apellidos, p.Nombre, d.Nombre,
                s.FechaInicio, s.FechaFin, e.Codigo, c.FechaCompra, c.TotalPagado));

    private IQueryable<ComprobanteDto> ComprobanteQuery() =>
        from c in db.Comprobantes.AsNoTracking()
        join e in db.Estados.AsNoTracking() on c.EstadoId equals e.EstadoId
        select new ComprobanteDto(Id(c.ComprobanteId), Id(c.CompraId), c.TipoComprobante,
            c.Serie, c.Numero, e.Codigo, c.FechaEmision, c.Subtotal, c.Impuestos, c.Total,
            db.ComprobanteDetalle.AsNoTracking().Where(d => d.ComprobanteId == c.ComprobanteId)
                .OrderBy(d => d.ComprobanteDetalleId)
                .Select(d => new ComprobanteLineaDto(Id(d.ComprobanteDetalleId), d.Descripcion,
                    d.Cantidad, d.PrecioUnitario, d.Importe)).ToArray());

    private IQueryable<TicketViajeDto> TicketQuery() =>
        from t in db.TicketsViaje.AsNoTracking()
        join p in db.Pasajeros.AsNoTracking() on t.PasajeroId equals p.PasajeroId
        join e in db.Estados.AsNoTracking() on t.EstadoId equals e.EstadoId
        select new TicketViajeDto(Id(t.TicketViajeId), t.CodigoTicket, Id(t.CompraId),
            Id(t.ReservaId), Id(t.PasajeroId), p.Nombres + " " + p.Apellidos,
            e.Codigo, t.EmitidoEn, t.UrlDocumento);

    private IQueryable<VoucherDto> VoucherQuery() =>
        from v in db.Vouchers.AsNoTracking()
        join e in db.Estados.AsNoTracking() on v.EstadoId equals e.EstadoId
        select new VoucherDto(Id(v.VoucherId), v.CodigoVoucher, Id(v.CompraId),
            Id(v.ReservaId), e.Codigo, v.EmitidoEn, v.UrlDocumento);

    private IQueryable<ResenaDto> ReviewQuery() =>
        from r in db.Resenas.AsNoTracking()
        join c in db.Clientes.AsNoTracking() on r.ClienteId equals c.ClienteId
        join d in db.Destinos.AsNoTracking() on r.DestinoId equals d.DestinoId
        join e in db.Estados.AsNoTracking() on r.EstadoModeracionId equals e.EstadoId
        select new ResenaDto(Id(r.ResenaId), c.Nombres + " " + c.Apellidos, d.Nombre,
            r.ReservaId == null ? null : Id(r.ReservaId.Value), r.Calificacion,
            r.Titulo, r.Comentario, e.Codigo, r.CreadoEn);

    private async Task<long> ClienteIdForUser(long usuarioId, CancellationToken ct) =>
        await db.Clientes.Where(x => x.UsuarioId == usuarioId).Select(x => (long?)x.ClienteId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, "Complete primero su perfil de cliente.");

    private async Task<int> StateId(string group, string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == group && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado {group}.{code} requerido en WayPathDB.");

    private async Task<string> StateCode(int stateId, CancellationToken ct) =>
        await db.Estados.Where(x => x.EstadoId == stateId).Select(x => x.Codigo).SingleAsync(ct);

    private async Task<string> NextReceiptNumber(CancellationToken ct)
    {
        var number = await db.Comprobantes.CountAsync(x => x.Serie == "B001", ct) + 1;
        string candidate;
        do { candidate = number++.ToString("00000000", System.Globalization.CultureInfo.InvariantCulture); }
        while (await db.Comprobantes.AnyAsync(x => x.Serie == "B001" && x.Numero == candidate, ct));
        return candidate;
    }

    private static async Task<string> NextCode(string prefix, Func<string, Task<bool>> exists)
    {
        string code;
        do { code = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}"; }
        while (await exists(code));
        return code;
    }

    private async Task SaveGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo emitir la venta porque los datos no cumplen una restriccion de WayPathDB."); }
    }

    private sealed record CompraRow(long CompraId, long ClienteId, DateTime FechaCompra, CompraResumenDto Dto);
}
