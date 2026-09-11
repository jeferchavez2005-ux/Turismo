using System.Data;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Cliente;
using WayPath.Domain.Ventas;

namespace WayPath.Infrastructure;

public sealed class ReservaStore(WayPathDbContext db) : IReservaStore
{
    private const int ReservationHoldMinutes = 30;
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<ReservaDetalleDto> Create(long usuarioId, CrearReservaRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        await ValidatePassengers(clienteId, request.Pasajeros, ct);

        var salida = await db.Salidas.SingleOrDefaultAsync(x => x.SalidaId == request.SalidaId, ct)
            ?? throw new SecurityFault(404, "Salida no encontrada.");
        var salidaState = await StateCode(salida.EstadoId, ct);
        if (salidaState != "PROGRAMADA")
            throw new SecurityFault(409, "La salida no esta disponible para reservas.");
        if (await IsBlocked(salida.PaqueteId, salida.SalidaId, salida.FechaInicio, salida.FechaFin, ct))
            throw new SecurityFault(409, "La salida tiene un bloqueo de disponibilidad.");

        var passengerCount = request.Pasajeros.Length;
        if (salida.CupoTotal - salida.CupoReservado < passengerCount)
            throw new SecurityFault(409, "No hay cupos suficientes para esta salida.");

        var tariff = await (from t in db.Tarifas
            join e in db.Estados on t.EstadoId equals e.EstadoId
            where t.TarifaId == request.TarifaId && t.PaqueteId == salida.PaqueteId && e.Codigo == "ACTIVO"
                && t.VigenciaInicio <= salida.FechaInicio
                && (t.VigenciaFin == null || t.VigenciaFin >= salida.FechaInicio)
            select t).SingleOrDefaultAsync(ct)
            ?? throw new SecurityFault(400, "La tarifa seleccionada no existe, no esta activa o no corresponde a la salida.");

        var subtotal = request.Pasajeros.Sum(p => PriceForPassenger(tariff.PrecioBase, tariff.PrecioNino, p.TipoPasajero));
        var reserveState = await ReservaStateId("PENDIENTE_PAGO", ct);
        var reserva = new Reserva
        {
            CodigoReserva = await NextCode("WP", ct),
            ClienteId = clienteId,
            SalidaId = salida.SalidaId,
            EstadoId = reserveState,
            MonedaId = tariff.MonedaId,
            FechaReserva = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(ReservationHoldMinutes),
            Subtotal = subtotal,
            Impuestos = 0,
            Descuento = 0,
            Total = subtotal,
            Observaciones = request.Observaciones
        };
        db.Reservas.Add(reserva);
        await SaveGuard(ct);

        db.ReservaDetalle.Add(new ReservaDetalle
        {
            ReservaId = reserva.ReservaId,
            PaqueteId = salida.PaqueteId,
            TarifaId = tariff.TarifaId,
            TipoDetalle = "PAQUETE",
            Descripcion = await PackageName(salida.PaqueteId, ct),
            Cantidad = passengerCount,
            PrecioUnitario = passengerCount == 0 ? 0 : decimal.Round(subtotal / passengerCount, 2),
            Importe = subtotal
        });
        db.ReservaPasajeros.AddRange(request.Pasajeros.Select(p => new ReservaPasajero
        {
            ReservaId = reserva.ReservaId,
            PasajeroId = p.PasajeroId,
            TipoPasajero = p.TipoPasajero,
            EsTitular = p.EsTitular
        }));
        db.ReservaHistorialEstados.Add(new ReservaHistorialEstado
        {
            ReservaId = reserva.ReservaId,
            EstadoNuevoId = reserveState,
            UsuarioId = usuarioId,
            Comentario = "Reserva creada por cliente.",
            CreadoEn = DateTime.UtcNow
        });
        salida.CupoReservado += passengerCount;
        await SaveGuard(ct);
        await tx.CommitAsync(ct);
        return await Detail(reserva.ReservaId, ct) ?? throw new SecurityFault(500, "No se pudo leer la reserva creada.");
    }

    public async Task<ReservaResumenDto[]> Mine(long usuarioId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        return await SummaryQuery().Where(x => x.ClienteId == Id(clienteId))
            .OrderByDescending(x => x.FechaReserva).ToArrayAsync(ct);
    }

    public async Task<ReservaDetalleDto?> DetailForUser(long usuarioId, long reservaId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var belongs = await db.Reservas.AsNoTracking().AnyAsync(x => x.ReservaId == reservaId && x.ClienteId == clienteId, ct);
        return belongs ? await Detail(reservaId, ct) : null;
    }

    public Task<ReservaDetalleDto?> DetailAdmin(long reservaId, CancellationToken ct) => Detail(reservaId, ct);

    public async Task CancelMine(long usuarioId, long reservaId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        await ChangeStateInternal(reservaId, clienteId, "CANCELADA", usuarioId, "Reserva cancelada por cliente.", true, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<ReservaDetalleDto> UpdatePassengers(long usuarioId, long reservaId,
        ActualizarPasajerosReservaRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var reserva = await db.Reservas.SingleOrDefaultAsync(x => x.ReservaId == reservaId && x.ClienteId == clienteId, ct)
            ?? throw new SecurityFault(404, "Reserva no encontrada.");
        var state = await StateCode(reserva.EstadoId, ct);
        if (state is not ("BORRADOR" or "PENDIENTE_PAGO"))
            throw new SecurityFault(409, "Los pasajeros solo pueden modificarse antes de confirmar la reserva.");
        await ValidatePassengers(clienteId, request.Pasajeros, ct);

        var current = await db.ReservaPasajeros.Where(x => x.ReservaId == reservaId).ToArrayAsync(ct);
        var delta = request.Pasajeros.Length - current.Length;
        if (delta != 0)
        {
            var salida = await db.Salidas.SingleAsync(x => x.SalidaId == reserva.SalidaId, ct);
            if (delta > 0 && salida.CupoTotal - salida.CupoReservado < delta)
                throw new SecurityFault(409, "No hay cupos suficientes para agregar pasajeros.");
            salida.CupoReservado += delta;
        }
        db.ReservaPasajeros.RemoveRange(current);
        db.ReservaPasajeros.AddRange(request.Pasajeros.Select(p => new ReservaPasajero
        {
            ReservaId = reservaId,
            PasajeroId = p.PasajeroId,
            TipoPasajero = p.TipoPasajero,
            EsTitular = p.EsTitular
        }));
        await RecalculateTotals(reserva, request.Pasajeros, ct);
        await SaveGuard(ct);
        await tx.CommitAsync(ct);
        return await Detail(reservaId, ct) ?? throw new SecurityFault(500, "No se pudo leer la reserva actualizada.");
    }

    public async Task<ReservaPage> List(ReservaFiltroRequest filtro, int page, int pageSize, CancellationToken ct)
    {
        var query = SummaryQuery()
            .Where(x => filtro.ClienteId == null || x.ClienteId == Id(filtro.ClienteId.Value))
            .Where(x => filtro.SalidaId == null || x.SalidaId == Id(filtro.SalidaId.Value))
            .Where(x => filtro.EstadoId == null || db.Reservas.Any(r => r.ReservaId.ToString() == x.ReservaId && r.EstadoId == filtro.EstadoId))
            .Where(x => filtro.Desde == null || DateOnly.FromDateTime(x.FechaReserva) >= filtro.Desde)
            .Where(x => filtro.Hasta == null || DateOnly.FromDateTime(x.FechaReserva) <= filtro.Hasta);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.FechaReserva)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task ChangeState(long reservaId, CambiarEstadoReservaRequest request, long actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await ChangeStateInternal(reservaId, null, request.Codigo, actorId, request.Comentario, false, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<PasajeroReservaDto[]> MyPassengers(long usuarioId, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        return await db.Pasajeros.AsNoTracking().Where(x => x.ClienteId == clienteId)
            .OrderBy(x => x.Apellidos).ThenBy(x => x.Nombres)
            .Select(x => new PasajeroReservaDto(Id(x.PasajeroId), x.Nombres, x.Apellidos,
                x.FechaNacimiento, x.RequerimientosEspeciales)).ToArrayAsync(ct);
    }

    public async Task<PasajeroReservaDto> CreatePassenger(long usuarioId, PasajeroReservaRequest request, CancellationToken ct)
    {
        var clienteId = await ClienteIdForUser(usuarioId, ct);
        var passenger = new Pasajero
        {
            ClienteId = clienteId,
            Nombres = request.Nombres,
            Apellidos = request.Apellidos,
            FechaNacimiento = request.FechaNacimiento,
            RequerimientosEspeciales = request.RequerimientosEspeciales,
            CreadoEn = DateTime.UtcNow
        };
        db.Pasajeros.Add(passenger);
        await SaveGuard(ct);
        return new(Id(passenger.PasajeroId), passenger.Nombres, passenger.Apellidos,
            passenger.FechaNacimiento, passenger.RequerimientosEspeciales);
    }

    private async Task ChangeStateInternal(long reservaId, long? clienteId, string code, long actorId,
        string? comment, bool ownerChange, CancellationToken ct)
    {
        var reserva = await db.Reservas.SingleOrDefaultAsync(x => x.ReservaId == reservaId
                && (clienteId == null || x.ClienteId == clienteId), ct)
            ?? throw new SecurityFault(404, "Reserva no encontrada.");
        var currentCode = await StateCode(reserva.EstadoId, ct);
        if (currentCode is "CANCELADA" or "COMPLETADA")
            throw new SecurityFault(409, "La reserva ya se encuentra cerrada.");
        if (ownerChange && currentCode == "CONFIRMADA")
            throw new SecurityFault(409, "Una reserva confirmada debe cancelarse por soporte o administracion.");

        var newState = await ReservaStateId(code, ct);
        if (newState == reserva.EstadoId) return;
        var previous = reserva.EstadoId;
        reserva.EstadoId = newState;
        db.ReservaHistorialEstados.Add(new ReservaHistorialEstado
        {
            ReservaId = reserva.ReservaId,
            EstadoAnteriorId = previous,
            EstadoNuevoId = newState,
            UsuarioId = actorId,
            Comentario = comment,
            CreadoEn = DateTime.UtcNow
        });
        if (code == "CANCELADA")
        {
            var salida = await db.Salidas.SingleAsync(x => x.SalidaId == reserva.SalidaId, ct);
            var passengers = await db.ReservaPasajeros.CountAsync(x => x.ReservaId == reserva.ReservaId, ct);
            salida.CupoReservado = Math.Max(0, salida.CupoReservado - passengers);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<ReservaDetalleDto?> Detail(long reservaId, CancellationToken ct)
    {
        var header = await (from r in db.Reservas.AsNoTracking()
            join c in db.Clientes.AsNoTracking() on r.ClienteId equals c.ClienteId
            join s in db.Salidas.AsNoTracking() on r.SalidaId equals s.SalidaId
            join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
            join d in db.Destinos.AsNoTracking() on p.DestinoId equals d.DestinoId
            join e in db.Estados.AsNoTracking() on r.EstadoId equals e.EstadoId
            join m in db.Monedas.AsNoTracking() on r.MonedaId equals m.MonedaId
            where r.ReservaId == reservaId
            select new { r, Cliente = c.Nombres + " " + c.Apellidos, s, PaqueteId = p.PaqueteId,
                Paquete = p.Nombre, Destino = d.Nombre, Estado = e.Codigo, Moneda = m.CodigoIso, m.Simbolo })
            .SingleOrDefaultAsync(ct);
        if (header is null) return null;
        var details = await db.ReservaDetalle.AsNoTracking().Where(x => x.ReservaId == reservaId)
            .OrderBy(x => x.ReservaDetalleId)
            .Select(x => new ReservaLineaDto(Id(x.ReservaDetalleId), x.TipoDetalle, x.Descripcion,
                x.Cantidad, x.PrecioUnitario, x.Importe,
                x.PaqueteId == null ? null : Id(x.PaqueteId.Value),
                x.TarifaId == null ? null : Id(x.TarifaId.Value))).ToArrayAsync(ct);
        var passengers = await (from rp in db.ReservaPasajeros.AsNoTracking()
            join p in db.Pasajeros.AsNoTracking() on rp.PasajeroId equals p.PasajeroId
            where rp.ReservaId == reservaId
            orderby rp.EsTitular descending, p.Apellidos, p.Nombres
            select new ReservaPasajeroDto(Id(rp.ReservaPasajeroId), Id(p.PasajeroId),
                p.Nombres, p.Apellidos, rp.TipoPasajero, rp.EsTitular)).ToArrayAsync(ct);
        var history = await HistoryQuery(reservaId).ToArrayAsync(ct);
        return new(Id(header.r.ReservaId), header.r.CodigoReserva, Id(header.r.ClienteId),
            header.Cliente, Id(header.r.SalidaId), Id(header.PaqueteId), header.Paquete,
            header.Destino, header.s.FechaInicio, header.s.FechaFin, header.Estado,
            header.r.FechaReserva, header.r.FechaExpiracion, header.r.Subtotal,
            header.r.Impuestos, header.r.Descuento, header.r.Total, header.Moneda,
            header.Simbolo, header.r.Observaciones, details, passengers, history);
    }

    private IQueryable<ReservaResumenDto> SummaryQuery() =>
        from r in db.Reservas.AsNoTracking()
        join c in db.Clientes.AsNoTracking() on r.ClienteId equals c.ClienteId
        join s in db.Salidas.AsNoTracking() on r.SalidaId equals s.SalidaId
        join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
        join d in db.Destinos.AsNoTracking() on p.DestinoId equals d.DestinoId
        join e in db.Estados.AsNoTracking() on r.EstadoId equals e.EstadoId
        join m in db.Monedas.AsNoTracking() on r.MonedaId equals m.MonedaId
        let passengerCount = db.ReservaPasajeros.Count(rp => rp.ReservaId == r.ReservaId)
        select new ReservaResumenDto(Id(r.ReservaId), r.CodigoReserva, Id(r.ClienteId),
            c.Nombres + " " + c.Apellidos, Id(r.SalidaId), p.Nombre, d.Nombre,
            s.FechaInicio, s.FechaFin, e.Codigo, r.FechaReserva, r.FechaExpiracion,
            r.Total, m.CodigoIso, passengerCount);

    private IQueryable<ReservaHistorialDto> HistoryQuery(long reservaId) =>
        from h in db.ReservaHistorialEstados.AsNoTracking()
        join en in db.Estados.AsNoTracking() on h.EstadoNuevoId equals en.EstadoId
        join ea0 in db.Estados.AsNoTracking() on h.EstadoAnteriorId equals ea0.EstadoId into previous
        from ea in previous.DefaultIfEmpty()
        where h.ReservaId == reservaId
        orderby h.CreadoEn
        select new ReservaHistorialDto(Id(h.ReservaHistorialEstadoId), en.Codigo,
            ea == null ? null : ea.Codigo, h.UsuarioId == null ? null : Id(h.UsuarioId.Value),
            h.Comentario, h.CreadoEn);

    private async Task RecalculateTotals(Reserva reserva, ReservaPasajeroRequest[] passengers, CancellationToken ct)
    {
        var line = await db.ReservaDetalle.SingleOrDefaultAsync(x => x.ReservaId == reserva.ReservaId && x.TipoDetalle == "PAQUETE", ct)
            ?? throw new SecurityFault(409, "La reserva no tiene detalle de paquete.");
        var tariff = await db.Tarifas.SingleAsync(x => x.TarifaId == line.TarifaId, ct);
        var subtotal = passengers.Sum(p => PriceForPassenger(tariff.PrecioBase, tariff.PrecioNino, p.TipoPasajero));
        line.Cantidad = passengers.Length;
        line.PrecioUnitario = decimal.Round(subtotal / passengers.Length, 2);
        line.Importe = subtotal;
        reserva.Subtotal = subtotal;
        reserva.Total = subtotal + reserva.Impuestos - reserva.Descuento;
    }

    private async Task ValidatePassengers(long clienteId, ReservaPasajeroRequest[] passengers, CancellationToken ct)
    {
        var ids = passengers.Select(x => x.PasajeroId).ToArray();
        var count = await db.Pasajeros.CountAsync(x => x.ClienteId == clienteId && ids.Contains(x.PasajeroId), ct);
        if (count != ids.Length)
            throw new SecurityFault(400, "Uno o mas pasajeros no pertenecen al cliente autenticado.");
    }

    private async Task<bool> IsBlocked(long paqueteId, long salidaId, DateOnly start, DateOnly end, CancellationToken ct) =>
        await db.BloqueosDisponibilidad.AnyAsync(x =>
            (x.SalidaId == salidaId || x.PaqueteId == paqueteId) &&
            x.FechaInicio <= end && x.FechaFin >= start, ct);

    private async Task<long> ClienteIdForUser(long usuarioId, CancellationToken ct) =>
        await db.Clientes.Where(x => x.UsuarioId == usuarioId).Select(x => (long?)x.ClienteId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, "Complete primero su perfil de cliente.");

    private async Task<int> ReservaStateId(string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == "RESERVA" && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado RESERVA.{code} requerido en WayPathDB.");

    private async Task<string> StateCode(int stateId, CancellationToken ct) =>
        await db.Estados.Where(x => x.EstadoId == stateId).Select(x => x.Codigo).SingleAsync(ct);

    private async Task<string> PackageName(long packageId, CancellationToken ct) =>
        await db.Paquetes.Where(x => x.PaqueteId == packageId).Select(x => x.Nombre).SingleAsync(ct);

    private async Task<string> NextCode(string prefix, CancellationToken ct)
    {
        string code;
        do { code = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}"; }
        while (await db.Reservas.AnyAsync(x => x.CodigoReserva == code, ct));
        return code;
    }

    private static decimal PriceForPassenger(decimal adultPrice, decimal? childPrice, string type) =>
        type is "NINO" or "INFANTE" ? childPrice ?? adultPrice : adultPrice;

    private async Task SaveGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new SecurityFault(409, "La reserva cambio durante la operacion. Vuelva a intentarlo."); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo guardar porque los datos no cumplen una restriccion de WayPathDB."); }
    }
}
