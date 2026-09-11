using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Pagos;
using WayPath.Domain.Ventas;

namespace WayPath.Infrastructure;

public sealed class PagoStore(WayPathDbContext db) : IPagoStore
{
    private const int PaymentOrderMinutes = 30;
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<MetodoPagoDto[]> Methods(CancellationToken ct) =>
        await db.MetodosPago.AsNoTracking().Where(x => x.EsActivo).OrderBy(x => x.MetodoPagoId)
            .Select(x => new MetodoPagoDto(x.MetodoPagoId, x.Codigo, x.Nombre, x.RequierePasarela))
            .ToArrayAsync(ct);

    public async Task<OrdenPagoDto> CreateOrder(long usuarioId, CrearOrdenPagoRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reserva = await ReservaForUser(usuarioId, request.ReservaId, tracking: true, ct)
            ?? throw new SecurityFault(404, "Reserva no encontrada.");
        var state = await StateCode(reserva.EstadoId, ct);
        if (state is "CANCELADA" or "COMPLETADA")
            throw new SecurityFault(409, "No se puede pagar una reserva cancelada o completada.");
        if (state == "CONFIRMADA")
            throw new SecurityFault(409, "La reserva ya fue confirmada.");
        if (reserva.Total <= 0)
            throw new SecurityFault(409, "La reserva no tiene un monto valido para pagar.");
        if (reserva.FechaExpiracion is not null && reserva.FechaExpiracion < DateTime.UtcNow)
            throw new SecurityFault(409, "La reserva expiro. Debe crear una nueva reserva.");

        var pendingState = await OrdenStateId("PENDIENTE", ct);
        var active = await db.OrdenesPago.SingleOrDefaultAsync(x => x.ReservaId == reserva.ReservaId
            && x.EstadoId == pendingState && (x.FechaExpiracion == null || x.FechaExpiracion >= DateTime.UtcNow), ct);
        if (active is not null)
        {
            await tx.CommitAsync(ct);
            return await FindOrder(active.OrdenPagoId, ct);
        }
        var order = new OrdenPago
        {
            ReservaId = reserva.ReservaId,
            MonedaId = reserva.MonedaId,
            EstadoId = pendingState,
            CodigoOrden = await NextOrderCode(ct),
            MontoTotal = reserva.Total,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(PaymentOrderMinutes)
        };
        db.OrdenesPago.Add(order);
        await SaveGuard(ct);
        await tx.CommitAsync(ct);
        return await FindOrder(order.OrdenPagoId, ct);
    }

    public async Task<PagoDto> RegisterAttempt(long usuarioId, RegistrarIntentoPagoRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.OrdenesPago.SingleOrDefaultAsync(x => x.OrdenPagoId == request.OrdenPagoId, ct)
            ?? throw new SecurityFault(404, "Orden de pago no encontrada.");
        var reserva = await ReservaForUser(usuarioId, order.ReservaId, tracking: true, ct)
            ?? throw new SecurityFault(404, "Reserva no encontrada.");
        if (order.ReservaId != reserva.ReservaId || order.MonedaId != reserva.MonedaId || order.MontoTotal != reserva.Total)
            throw new SecurityFault(409, "La orden de pago no coincide con la reserva.");

        var reserveState = await StateCode(reserva.EstadoId, ct);
        if (reserveState is "CANCELADA" or "COMPLETADA")
            throw new SecurityFault(409, "No se puede pagar una reserva cancelada o completada.");
        if (reserveState == "CONFIRMADA")
            throw new SecurityFault(409, "La reserva ya fue confirmada.");
        var orderState = await StateCode(order.EstadoId, ct);
        if (orderState != "PENDIENTE")
            throw new SecurityFault(409, "La orden de pago no esta pendiente.");
        if (order.FechaExpiracion is not null && order.FechaExpiracion < DateTime.UtcNow)
        {
            order.EstadoId = await OrdenStateId("EXPIRADA", ct);
            db.PagoIntentos.Add(NewAttempt(order, request.MetodoPagoId, null, "EXPIRADO", "La orden de pago expiro."));
            await SaveGuard(ct);
            throw new SecurityFault(409, "La orden de pago expiro.");
        }
        var method = await db.MetodosPago.SingleOrDefaultAsync(x => x.MetodoPagoId == request.MetodoPagoId && x.EsActivo, ct)
            ?? throw new SecurityFault(400, "Metodo de pago no disponible.");
        var approvedState = await PagoStateId("APROBADO", ct);
        if (await db.Pagos.AnyAsync(x => x.ReservaId == reserva.ReservaId && x.EstadoId == approvedState, ct))
            throw new SecurityFault(409, "La reserva ya tiene un pago aprobado.");

        var finalPaymentState = request.ResultadoSimulado switch
        {
            "APROBADO" => approvedState,
            "RECHAZADO" => await PagoStateId("RECHAZADO", ct),
            _ => await PagoStateId("PENDIENTE", ct)
        };
        var pago = new Pago
        {
            OrdenPagoId = order.OrdenPagoId,
            ReservaId = reserva.ReservaId,
            MetodoPagoId = method.MetodoPagoId,
            MonedaId = reserva.MonedaId,
            EstadoId = finalPaymentState,
            CodigoTransaccion = request.ResultadoSimulado == "APROBADO" ? await NextTransactionCode(ct) : null,
            Monto = order.MontoTotal,
            FechaPago = request.ResultadoSimulado == "APROBADO" ? DateTime.UtcNow : null,
            ReferenciaPasarela = $"SIM-{request.ResultadoSimulado}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Observaciones = request.Mensaje,
            CreadoEn = DateTime.UtcNow
        };
        db.Pagos.Add(pago);
        await SaveGuard(ct);
        db.PagoIntentos.Add(NewAttempt(order, method.MetodoPagoId, pago.PagoId, request.ResultadoSimulado,
            request.Mensaje ?? SimulatedMessage(request.ResultadoSimulado)));

        if (request.ResultadoSimulado == "APROBADO")
        {
            order.EstadoId = await OrdenStateId("APROBADA", ct);
            var previous = reserva.EstadoId;
            reserva.EstadoId = await ReservaStateId("CONFIRMADA", ct);
            db.ReservaHistorialEstados.Add(new ReservaHistorialEstado
            {
                ReservaId = reserva.ReservaId,
                EstadoAnteriorId = previous,
                EstadoNuevoId = reserva.EstadoId,
                UsuarioId = usuarioId,
                Comentario = "Reserva confirmada por pago aprobado.",
                CreadoEn = DateTime.UtcNow
            });
        }
        await SaveGuard(ct);
        await tx.CommitAsync(ct);
        return await FindPayment(pago.PagoId, ct);
    }

    public async Task<EstadoPagoDto> Status(long usuarioId, long reservaId, CancellationToken ct)
    {
        var reserva = await ReservaForUser(usuarioId, reservaId, tracking: false, ct)
            ?? throw new SecurityFault(404, "Reserva no encontrada.");
        var reserveState = await StateCode(reserva.EstadoId, ct);
        var order = await OrderQuery().Where(x => x.ReservaId == Id(reserva.ReservaId))
            .OrderByDescending(x => x.FechaCreacion).FirstOrDefaultAsync(ct);
        var payment = await PaymentQuery().Where(x => x.ReservaId == Id(reserva.ReservaId))
            .OrderByDescending(x => x.CreadoEn).FirstOrDefaultAsync(ct);
        var attempts = order is null ? [] : await AttemptQuery(order.OrdenPagoId).ToArrayAsync(ct);
        return new(Id(reserva.ReservaId), reserva.CodigoReserva, reserveState, order, payment, attempts);
    }

    public async Task<PagoDto[]> Mine(long usuarioId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        return await PaymentQuery().Where(x => db.Reservas.Any(r => r.ReservaId.ToString() == x.ReservaId && r.ClienteId == clienteId))
            .OrderByDescending(x => x.CreadoEn).ToArrayAsync(ct);
    }

    public async Task<PagoPage> ListPayments(int page, int pageSize, CancellationToken ct)
    {
        var query = PaymentQuery();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<OrdenPagoPage> ListOrders(int page, int pageSize, CancellationToken ct)
    {
        var query = OrderQuery();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.FechaCreacion).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task ChangePaymentState(long pagoId, CambiarEstadoPagoRequest request, long actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var payment = await db.Pagos.SingleOrDefaultAsync(x => x.PagoId == pagoId, ct)
            ?? throw new SecurityFault(404, "Pago no encontrado.");
        payment.EstadoId = await PagoStateId(request.Codigo, ct);
        payment.Observaciones = request.Observaciones;
        if (request.Codigo == "APROBADO" && payment.FechaPago is null)
        {
            payment.FechaPago = DateTime.UtcNow;
            payment.CodigoTransaccion ??= await NextTransactionCode(ct);
            var order = await db.OrdenesPago.SingleAsync(x => x.OrdenPagoId == payment.OrdenPagoId, ct);
            var reserva = await db.Reservas.SingleAsync(x => x.ReservaId == payment.ReservaId, ct);
            order.EstadoId = await OrdenStateId("APROBADA", ct);
            var previous = reserva.EstadoId;
            reserva.EstadoId = await ReservaStateId("CONFIRMADA", ct);
            db.ReservaHistorialEstados.Add(new ReservaHistorialEstado
            {
                ReservaId = reserva.ReservaId,
                EstadoAnteriorId = previous,
                EstadoNuevoId = reserva.EstadoId,
                UsuarioId = actorId,
                Comentario = "Reserva confirmada por cambio administrativo de pago.",
                CreadoEn = DateTime.UtcNow
            });
        }
        await SaveGuard(ct);
        await tx.CommitAsync(ct);
    }

    private IQueryable<OrdenPagoDto> OrderQuery() =>
        from o in db.OrdenesPago.AsNoTracking()
        join m in db.Monedas.AsNoTracking() on o.MonedaId equals m.MonedaId
        join e in db.Estados.AsNoTracking() on o.EstadoId equals e.EstadoId
        select new OrdenPagoDto(Id(o.OrdenPagoId), Id(o.ReservaId), o.CodigoOrden,
            o.MontoTotal, m.CodigoIso, e.Codigo, o.FechaCreacion, o.FechaExpiracion);

    private IQueryable<PagoDto> PaymentQuery() =>
        from p in db.Pagos.AsNoTracking()
        join metodo in db.MetodosPago.AsNoTracking() on p.MetodoPagoId equals metodo.MetodoPagoId
        join estado in db.Estados.AsNoTracking() on p.EstadoId equals estado.EstadoId
        join moneda in db.Monedas.AsNoTracking() on p.MonedaId equals moneda.MonedaId
        select new PagoDto(Id(p.PagoId), Id(p.OrdenPagoId), Id(p.ReservaId), metodo.Codigo,
            estado.Codigo, p.Monto, moneda.CodigoIso, p.CodigoTransaccion, p.ReferenciaPasarela,
            p.FechaPago, p.CreadoEn, p.Observaciones);

    private IQueryable<PagoIntentoDto> AttemptQuery(string orderId) =>
        from i in db.PagoIntentos.AsNoTracking()
        join m in db.MetodosPago.AsNoTracking() on i.MetodoPagoId equals m.MetodoPagoId
        where i.OrdenPagoId.ToString() == orderId
        orderby i.CreadoEn descending
        select new PagoIntentoDto(Id(i.PagoIntentoId), Id(i.OrdenPagoId),
            i.PagoId == null ? null : Id(i.PagoId.Value), m.Codigo, i.Resultado, i.Mensaje, i.CreadoEn);

    private async Task<OrdenPagoDto> FindOrder(long orderId, CancellationToken ct) =>
        await OrderQuery().SingleAsync(x => x.OrdenPagoId == Id(orderId), ct);

    private async Task<PagoDto> FindPayment(long pagoId, CancellationToken ct) =>
        await PaymentQuery().SingleAsync(x => x.PagoId == Id(pagoId), ct);

    private PagoIntento NewAttempt(OrdenPago order, int methodId, long? paymentId, string result, string? message) =>
        new()
        {
            OrdenPagoId = order.OrdenPagoId,
            PagoId = paymentId,
            MetodoPagoId = methodId,
            Resultado = result,
            Mensaje = message,
            PayloadRespuesta = JsonSerializer.Serialize(new { provider = "WayPathSim", result, at = DateTime.UtcNow }),
            CreadoEn = DateTime.UtcNow
        };

    private async Task<Reserva?> ReservaForUser(long usuarioId, long reservaId, bool tracking, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var query = tracking ? db.Reservas : db.Reservas.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.ReservaId == reservaId && x.ClienteId == clienteId, ct);
    }

    private async Task<long> ClienteIdForUser(long usuarioId, CancellationToken ct) =>
        await db.Clientes.Where(x => x.UsuarioId == usuarioId).Select(x => (long?)x.ClienteId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, "Complete primero su perfil de cliente.");

    private async Task<int> PagoStateId(string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == "PAGO" && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado PAGO.{code} requerido en WayPathDB.");

    private async Task<int> OrdenStateId(string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == "ORDEN_PAGO" && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado ORDEN_PAGO.{code} requerido en WayPathDB.");

    private async Task<int> ReservaStateId(string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == "RESERVA" && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado RESERVA.{code} requerido en WayPathDB.");

    private async Task<string> StateCode(int stateId, CancellationToken ct) =>
        await db.Estados.Where(x => x.EstadoId == stateId).Select(x => x.Codigo).SingleAsync(ct);

    private async Task<string> NextOrderCode(CancellationToken ct)
    {
        string code;
        do { code = $"OP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}"; }
        while (await db.OrdenesPago.AnyAsync(x => x.CodigoOrden == code, ct));
        return code;
    }

    private async Task<string> NextTransactionCode(CancellationToken ct)
    {
        string code;
        do { code = $"TRX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"; }
        while (await db.Pagos.AnyAsync(x => x.CodigoTransaccion == code, ct));
        return code;
    }

    private static string SimulatedMessage(string result) => result switch
    {
        "APROBADO" => "Pago aprobado por pasarela simulada.",
        "RECHAZADO" => "Pago rechazado por pasarela simulada.",
        _ => "Error simulado de pasarela."
    };

    private async Task SaveGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new SecurityFault(409, "El pago cambio durante la operacion. Vuelva a intentarlo."); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo guardar porque los datos no cumplen una restriccion de WayPathDB."); }
    }
}
