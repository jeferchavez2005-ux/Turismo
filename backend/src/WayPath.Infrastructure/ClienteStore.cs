using System.Data;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Cliente;

namespace WayPath.Infrastructure;

public sealed class ClienteStore(WayPathDbContext db) : IClienteStore
{
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<ClientePerfilDto?> GetByUser(long usuarioId, CancellationToken ct)
    {
        var row = await (from c in db.Clientes.AsNoTracking()
            join u in db.Usuarios.AsNoTracking() on c.UsuarioId equals u.UsuarioId
            join e in db.Estados.AsNoTracking() on c.EstadoId equals e.EstadoId
            where c.UsuarioId == usuarioId
            select new { c, u.Email, u.NombreUsuario, Estado = e.Codigo }).SingleOrDefaultAsync(ct);
        return row is null ? null : ToDto(row.c, row.Email, row.NombreUsuario, row.Estado);
    }

    public async Task<ClientePerfilDto> UpsertForUser(long usuarioId, ClientePerfilRequest request, CancellationToken ct)
    {
        var user = await db.Usuarios.SingleOrDefaultAsync(x => x.UsuarioId == usuarioId, ct)
            ?? throw new SecurityFault(404, "Usuario no encontrado.");
        var client = await db.Clientes.SingleOrDefaultAsync(x => x.UsuarioId == usuarioId, ct);
        if (client is null)
        {
            client = new ClientePerfil
            {
                UsuarioId = usuarioId,
                EstadoId = await ClienteStateId("ACTIVO", ct),
                CreadoEn = DateTime.UtcNow
            };
            db.Clientes.Add(client);
        }
        client.Nombres = request.Nombres;
        client.Apellidos = request.Apellidos;
        client.Telefono = request.Telefono;
        client.FechaNacimiento = request.FechaNacimiento;
        client.Genero = request.Genero;
        client.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var state = await db.Estados.AsNoTracking().Where(x => x.EstadoId == client.EstadoId)
            .Select(x => x.Codigo).SingleAsync(ct);
        return ToDto(client, user.Email, user.NombreUsuario, state);
    }

    public async Task<ClientePage> List(int page, int pageSize, CancellationToken ct)
    {
        var total = await db.Clientes.CountAsync(ct);
        var rows = await (from c in db.Clientes.AsNoTracking()
            join u in db.Usuarios.AsNoTracking() on c.UsuarioId equals u.UsuarioId
            join e in db.Estados.AsNoTracking() on c.EstadoId equals e.EstadoId
            orderby c.ClienteId
            select new { c, u.Email, Estado = e.Codigo })
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(rows.Select(x => new ClienteResumenDto(Id(x.c.ClienteId), Id(x.c.UsuarioId),
            x.Email, x.Estado, x.c.Nombres, x.c.Apellidos, x.c.Telefono, x.c.CreadoEn)).ToArray(),
            page, pageSize, total);
    }

    public async Task SetState(long clienteId, string codigo, CancellationToken ct)
    {
        var stateId = await ClienteStateId(codigo.Trim().ToUpperInvariant(), ct);
        var client = await db.Clientes.SingleOrDefaultAsync(x => x.ClienteId == clienteId, ct)
            ?? throw new SecurityFault(404, "Cliente no encontrado.");
        client.EstadoId = stateId;
        client.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<DocumentoClienteDto[]> ListDocuments(long usuarioId, CancellationToken ct) =>
        await DocumentQuery(await ClienteIdForUser(usuarioId, ct)).ToArrayAsync(ct);

    public async Task<DocumentoClienteDto> AddDocument(long usuarioId, DocumentoClienteRequest request, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        await ValidateDocument(request, null, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var isFirst = !await db.DocumentosCliente.AnyAsync(x => x.ClienteId == clientId, ct);
        if (request.EsPrincipal || isFirst) await ClearPrincipalDocuments(clientId, null, ct);
        var document = new DocumentoCliente
        {
            ClienteId = clientId,
            TipoDocumentoId = request.TipoDocumentoId,
            NumeroDocumento = request.NumeroDocumento,
            PaisEmisionId = request.PaisEmisionId,
            FechaVencimiento = request.FechaVencimiento,
            EsPrincipal = request.EsPrincipal || isFirst
        };
        db.DocumentosCliente.Add(document);
        await SaveWithDuplicateGuard(ct);
        await tx.CommitAsync(ct);
        return await DocumentQuery(clientId, document.DocumentoClienteId).SingleAsync(ct);
    }

    public async Task<DocumentoClienteDto> UpdateDocument(long usuarioId, long documentId, DocumentoClienteRequest request, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        await ValidateDocument(request, documentId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await db.DocumentosCliente.SingleOrDefaultAsync(x => x.ClienteId == clientId && x.DocumentoClienteId == documentId, ct)
            ?? throw new SecurityFault(404, "Documento no encontrado.");
        if (request.EsPrincipal) await ClearPrincipalDocuments(clientId, documentId, ct);
        document.TipoDocumentoId = request.TipoDocumentoId;
        document.NumeroDocumento = request.NumeroDocumento;
        document.PaisEmisionId = request.PaisEmisionId;
        document.FechaVencimiento = request.FechaVencimiento;
        document.EsPrincipal = request.EsPrincipal;
        await SaveWithDuplicateGuard(ct);
        await tx.CommitAsync(ct);
        return await DocumentQuery(clientId, documentId).SingleAsync(ct);
    }

    public async Task DeleteDocument(long usuarioId, long documentId, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await db.DocumentosCliente.SingleOrDefaultAsync(x => x.ClienteId == clientId && x.DocumentoClienteId == documentId, ct)
            ?? throw new SecurityFault(404, "Documento no encontrado.");
        var wasPrincipal = document.EsPrincipal;
        db.DocumentosCliente.Remove(document);
        await db.SaveChangesAsync(ct);
        if (wasPrincipal)
        {
            var next = await db.DocumentosCliente.Where(x => x.ClienteId == clientId)
                .OrderBy(x => x.DocumentoClienteId).FirstOrDefaultAsync(ct);
            if (next is not null)
            {
                next.EsPrincipal = true;
                await db.SaveChangesAsync(ct);
            }
        }
        await tx.CommitAsync(ct);
    }

    public async Task<DireccionClienteDto[]> ListAddresses(long usuarioId, CancellationToken ct) =>
        await AddressQuery(await ClienteIdForUser(usuarioId, ct)).ToArrayAsync(ct);

    public async Task<DireccionClienteDto> AddAddress(long usuarioId, DireccionClienteRequest request, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        await ValidateCity(request.CiudadId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var isFirst = !await db.DireccionesCliente.AnyAsync(x => x.ClienteId == clientId, ct);
        if (request.EsPrincipal || isFirst) await ClearPrincipalAddresses(clientId, null, ct);
        var address = new DireccionCliente
        {
            ClienteId = clientId,
            CiudadId = request.CiudadId,
            LineaDireccion = request.LineaDireccion,
            CodigoPostal = request.CodigoPostal,
            EsPrincipal = request.EsPrincipal || isFirst
        };
        db.DireccionesCliente.Add(address);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await AddressQuery(clientId, address.DireccionClienteId).SingleAsync(ct);
    }

    public async Task<DireccionClienteDto> UpdateAddress(long usuarioId, long addressId, DireccionClienteRequest request, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        await ValidateCity(request.CiudadId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var address = await db.DireccionesCliente.SingleOrDefaultAsync(x => x.ClienteId == clientId && x.DireccionClienteId == addressId, ct)
            ?? throw new SecurityFault(404, "Direccion no encontrada.");
        if (request.EsPrincipal) await ClearPrincipalAddresses(clientId, addressId, ct);
        address.CiudadId = request.CiudadId;
        address.LineaDireccion = request.LineaDireccion;
        address.CodigoPostal = request.CodigoPostal;
        address.EsPrincipal = request.EsPrincipal;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await AddressQuery(clientId, addressId).SingleAsync(ct);
    }

    public async Task DeleteAddress(long usuarioId, long addressId, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var address = await db.DireccionesCliente.SingleOrDefaultAsync(x => x.ClienteId == clientId && x.DireccionClienteId == addressId, ct)
            ?? throw new SecurityFault(404, "Direccion no encontrada.");
        var wasPrincipal = address.EsPrincipal;
        db.DireccionesCliente.Remove(address);
        await db.SaveChangesAsync(ct);
        if (wasPrincipal)
        {
            var next = await db.DireccionesCliente.Where(x => x.ClienteId == clientId)
                .OrderBy(x => x.DireccionClienteId).FirstOrDefaultAsync(ct);
            if (next is not null)
            {
                next.EsPrincipal = true;
                await db.SaveChangesAsync(ct);
            }
        }
        await tx.CommitAsync(ct);
    }

    public async Task<PreferenciaClienteDto?> GetPreferences(long usuarioId, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        return await PreferencesQuery(clientId).SingleOrDefaultAsync(ct);
    }

    public async Task<PreferenciaClienteDto> UpsertPreferences(long usuarioId, PreferenciaClienteRequest request, CancellationToken ct)
    {
        var clientId = await ClienteIdForUser(usuarioId, ct);
        if (request.MonedaPreferidaId is not null &&
            !await db.Monedas.AnyAsync(x => x.MonedaId == request.MonedaPreferidaId && x.EsActivo, ct))
            throw new SecurityFault(400, "La moneda seleccionada no existe o esta inactiva.");
        var pref = await db.PreferenciasCliente.SingleOrDefaultAsync(x => x.ClienteId == clientId, ct);
        if (pref is null)
        {
            pref = new PreferenciaCliente { ClienteId = clientId };
            db.PreferenciasCliente.Add(pref);
        }
        pref.IdiomaPreferido = request.IdiomaPreferido;
        pref.MonedaPreferidaId = request.MonedaPreferidaId;
        pref.RecibePromociones = request.RecibePromociones;
        pref.RequiereAccesibilidad = request.RequiereAccesibilidad;
        pref.Observaciones = request.Observaciones;
        await db.SaveChangesAsync(ct);
        return await PreferencesQuery(clientId).SingleAsync(ct);
    }

    public async Task<ClienteCatalogosDto> Catalogs(CancellationToken ct)
    {
        var tipos = await db.TiposDocumento.AsNoTracking().Where(x => x.EsActivo).OrderBy(x => x.TipoDocumentoId)
            .Select(x => new TipoDocumentoDto(x.TipoDocumentoId, x.Codigo, x.Nombre, x.LongitudMinima, x.LongitudMaxima)).ToArrayAsync(ct);
        var paises = await db.Paises.AsNoTracking().Where(x => x.EsActivo).OrderBy(x => x.Nombre)
            .Select(x => new PaisDto(x.PaisId, x.CodigoIso2, x.Nombre)).ToArrayAsync(ct);
        var ciudades = await (from c in db.Ciudades.AsNoTracking()
            join r in db.Regiones.AsNoTracking() on c.RegionId equals r.RegionId
            join p in db.Paises.AsNoTracking() on r.PaisId equals p.PaisId
            where c.EsActivo && r.EsActivo && p.EsActivo
            orderby p.Nombre, r.Nombre, c.Nombre
            select new CiudadDto(c.CiudadId, c.Nombre, r.Nombre, p.Nombre)).ToArrayAsync(ct);
        var monedas = await db.Monedas.AsNoTracking().Where(x => x.EsActivo).OrderByDescending(x => x.EsMonedaBase).ThenBy(x => x.CodigoIso)
            .Select(x => new MonedaDto(x.MonedaId, x.CodigoIso, x.Nombre, x.Simbolo, x.EsMonedaBase)).ToArrayAsync(ct);
        return new(tipos, paises, ciudades, monedas);
    }

    private async Task<long> ClienteIdForUser(long usuarioId, CancellationToken ct) =>
        await db.Clientes.Where(x => x.UsuarioId == usuarioId).Select(x => (long?)x.ClienteId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, "Complete primero su perfil de cliente.");

    private async Task<int> ClienteStateId(string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == "CLIENTE" && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, "No existe el estado de cliente requerido en WayPathDB.");

    private async Task ValidateDocument(DocumentoClienteRequest request, long? excludingDocumentId, CancellationToken ct)
    {
        var type = await db.TiposDocumento.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TipoDocumentoId == request.TipoDocumentoId && x.EsActivo, ct)
            ?? throw new SecurityFault(400, "El tipo de documento no existe o esta inactivo.");
        if (type.LongitudMinima is not null && request.NumeroDocumento.Length < type.LongitudMinima)
            throw new SecurityFault(400, "El numero de documento es demasiado corto.");
        if (type.LongitudMaxima is not null && request.NumeroDocumento.Length > type.LongitudMaxima)
            throw new SecurityFault(400, "El numero de documento es demasiado largo.");
        if (request.PaisEmisionId is not null &&
            !await db.Paises.AnyAsync(x => x.PaisId == request.PaisEmisionId && x.EsActivo, ct))
            throw new SecurityFault(400, "El pais de emision no existe o esta inactivo.");
        if (await db.DocumentosCliente.AnyAsync(x => x.TipoDocumentoId == request.TipoDocumentoId
            && x.NumeroDocumento == request.NumeroDocumento && x.DocumentoClienteId != excludingDocumentId, ct))
            throw new SecurityFault(409, "Ya existe un documento registrado con ese tipo y numero.");
    }

    private async Task ValidateCity(int ciudadId, CancellationToken ct)
    {
        var active = await (from c in db.Ciudades
            join r in db.Regiones on c.RegionId equals r.RegionId
            join p in db.Paises on r.PaisId equals p.PaisId
            where c.CiudadId == ciudadId && c.EsActivo && r.EsActivo && p.EsActivo
            select c.CiudadId).AnyAsync(ct);
        if (!active) throw new SecurityFault(400, "La ciudad seleccionada no existe o esta inactiva.");
    }

    private Task ClearPrincipalDocuments(long clientId, long? keepId, CancellationToken ct) =>
        db.DocumentosCliente.Where(x => x.ClienteId == clientId && x.DocumentoClienteId != keepId && x.EsPrincipal)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EsPrincipal, false), ct);

    private Task ClearPrincipalAddresses(long clientId, long? keepId, CancellationToken ct) =>
        db.DireccionesCliente.Where(x => x.ClienteId == clientId && x.DireccionClienteId != keepId && x.EsPrincipal)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EsPrincipal, false), ct);

    private IQueryable<DocumentoClienteDto> DocumentQuery(long clientId, long? documentId = null) =>
        from d in db.DocumentosCliente.AsNoTracking()
        join t in db.TiposDocumento.AsNoTracking() on d.TipoDocumentoId equals t.TipoDocumentoId
        join p in db.Paises.AsNoTracking() on d.PaisEmisionId equals p.PaisId into paisJoin
        from p in paisJoin.DefaultIfEmpty()
        where d.ClienteId == clientId && (documentId == null || d.DocumentoClienteId == documentId)
        orderby d.EsPrincipal descending, d.DocumentoClienteId
        select new DocumentoClienteDto(Id(d.DocumentoClienteId), d.TipoDocumentoId,
            t.Nombre, d.NumeroDocumento, d.PaisEmisionId, p == null ? null : p.Nombre,
            d.FechaVencimiento, d.EsPrincipal);

    private IQueryable<DireccionClienteDto> AddressQuery(long clientId, long? addressId = null) =>
        from d in db.DireccionesCliente.AsNoTracking()
        join c in db.Ciudades.AsNoTracking() on d.CiudadId equals c.CiudadId
        join r in db.Regiones.AsNoTracking() on c.RegionId equals r.RegionId
        join p in db.Paises.AsNoTracking() on r.PaisId equals p.PaisId
        where d.ClienteId == clientId && (addressId == null || d.DireccionClienteId == addressId)
        orderby d.EsPrincipal descending, d.DireccionClienteId
        select new DireccionClienteDto(Id(d.DireccionClienteId), d.CiudadId, c.Nombre, r.Nombre,
            p.Nombre, d.LineaDireccion, d.CodigoPostal, d.EsPrincipal);

    private IQueryable<PreferenciaClienteDto> PreferencesQuery(long clientId) =>
        from pref in db.PreferenciasCliente.AsNoTracking()
        join m in db.Monedas.AsNoTracking() on pref.MonedaPreferidaId equals m.MonedaId into monedaJoin
        from m in monedaJoin.DefaultIfEmpty()
        where pref.ClienteId == clientId
        select new PreferenciaClienteDto(Id(pref.PreferenciaClienteId), pref.IdiomaPreferido,
            pref.MonedaPreferidaId, m == null ? null : m.CodigoIso, pref.RecibePromociones,
            pref.RequiereAccesibilidad, pref.Observaciones);

    private static ClientePerfilDto ToDto(ClientePerfil client, string email, string? userName, string state) =>
        new(Id(client.ClienteId), Id(client.UsuarioId), email, userName, state, client.Nombres,
            client.Apellidos, client.Telefono, client.FechaNacimiento, client.Genero,
            Convert.ToBase64String(client.FilaVersion));

    private async Task SaveWithDuplicateGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo guardar porque los datos ya existen."); }
    }
}
