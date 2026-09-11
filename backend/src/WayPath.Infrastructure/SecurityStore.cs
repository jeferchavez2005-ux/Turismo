using System.Data;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Seguridad;

namespace WayPath.Infrastructure;

public sealed class SecurityStore(WayPathDbContext db) : ISecurityStore
{
    public Task<Usuario?> FindByEmail(string email, CancellationToken ct) =>
        db.Usuarios.SingleOrDefaultAsync(x => x.Email == email, ct);
    public Task<Usuario?> FindById(long id, CancellationToken ct) =>
        db.Usuarios.SingleOrDefaultAsync(x => x.UsuarioId == id, ct);
    public Task<bool> IsActive(Usuario user, CancellationToken ct) =>
        db.Estados.AnyAsync(x => x.EstadoId == user.EstadoId && x.Grupo == "USUARIO"
            && x.Codigo == "ACTIVO" && x.EsActivo, ct);

    public async Task<UserAccess?> GetAccess(long id, string sessionId, CancellationToken ct)
    {
        var user = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(x => x.UsuarioId == id, ct);
        if (user is null || !await IsActive(user, ct) ||
            !await db.Sesiones.AnyAsync(x => x.UsuarioId == id &&
                x.IdentificadorSesion == sessionId && x.FinalizadaEn == null, ct))
            return null;
        return await Access(user, ct);
    }

    private async Task<UserAccess> Access(Usuario user, CancellationToken ct)
    {
        var roles = await (from ur in db.UsuarioRoles
            join r in db.Roles on ur.RolId equals r.RolId
            where ur.UsuarioId == user.UsuarioId && r.EsActivo select r).AsNoTracking().ToArrayAsync(ct);
        var ids = roles.Select(x => x.RolId).ToArray();
        var permissions = await (from rp in db.RolPermisos
            join p in db.Permisos on rp.PermisoId equals p.PermisoId
            where ids.Contains(rp.RolId) select p.Codigo).Distinct().ToArrayAsync(ct);
        return new(user.UsuarioId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            user.Email, user.NombreUsuario, roles.Select(x => x.Codigo).ToArray(), permissions);
    }

    private async Task<int> StateId(string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == "USUARIO" && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, "No existe el estado de usuario requerido en WayPathDB.");

    public async Task<long> CreateUser(Usuario user, bool firstAdministrator, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (firstAdministrator && await (
            from ur in db.UsuarioRoles join r in db.Roles on ur.RolId equals r.RolId
            where r.Codigo == "ADMINISTRADOR" select ur.UsuarioId).AnyAsync(ct))
            throw new SecurityFault(409, "Ya existe un administrador. Use la administracion autenticada.");
        if (await db.Usuarios.AnyAsync(x => x.Email == user.Email, ct))
            throw new SecurityFault(409, "No se pudo registrar la cuenta con esos datos.");
        var roleCode = firstAdministrator ? "ADMINISTRADOR" : "CLIENTE";
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Codigo == roleCode && x.EsActivo, ct)
            ?? throw new SecurityFault(409, "Falta el rol requerido en WayPathDB.");
        user.EstadoId = await StateId("ACTIVO", ct);
        user.EmailConfirmado = false;
        db.Usuarios.Add(user);
        await db.SaveChangesAsync(ct);
        db.UsuarioRoles.Add(new() { UsuarioId = user.UsuarioId, RolId = role.RolId, AsignadoEn = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return user.UsuarioId;
    }

    public async Task<LoginSession> StartSession(Usuario user, string? ip, string? agent, CancellationToken ct)
    {
        if (!await IsActive(user, ct)) throw new SecurityFault(401, "Cuenta no disponible.");
        var sid = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        user.UltimoAccesoEn = now;
        db.Sesiones.Add(new()
        {
            UsuarioId = user.UsuarioId, IdentificadorSesion = sid,
            Ip = ip is { Length: <= 45 } ? ip : null,
            UserAgent = agent is null ? null : agent[..Math.Min(agent.Length, 500)],
            IniciadaEn = now
        });
        await db.SaveChangesAsync(ct);
        return new(await Access(user, ct), sid);
    }

    public async Task EndSession(long id, string sessionId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.Sesiones.Where(x => x.UsuarioId == id && x.IdentificadorSesion == sessionId
            && x.FinalizadaEn == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.FinalizadaEn, now), ct);
    }

    public async Task UpdateProfile(long id, string name, CancellationToken ct)
    {
        var user = await FindById(id, ct) ?? throw new SecurityFault(404, "Usuario no encontrado.");
        user.NombreUsuario = name;
        user.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangePassword(Usuario user, string hash, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        user.PasswordHash = hash;
        user.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RevokeSessions(user.UsuarioId, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<UserPage> ListUsers(int page, int pageSize, CancellationToken ct)
    {
        var total = await db.Usuarios.CountAsync(ct);
        var rows = await (from u in db.Usuarios.AsNoTracking()
            join e in db.Estados on u.EstadoId equals e.EstadoId
            orderby u.UsuarioId
            select new { u.UsuarioId, u.Email, u.NombreUsuario, Estado = e.Codigo, u.EmailConfirmado, u.FilaVersion })
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(rows.Select(x => new UserSummary(x.UsuarioId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            x.Email, x.NombreUsuario, x.Estado, x.EmailConfirmado, Convert.ToBase64String(x.FilaVersion))).ToArray(),
            page, pageSize, total);
    }

    public async Task<RoleSummary[]> ListRoles(CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(x => x.RolId).ToArrayAsync(ct);
        var grants = await db.RolPermisos.AsNoTracking().ToArrayAsync(ct);
        return roles.Select(r => new RoleSummary(r.RolId, r.Codigo, r.Nombre, r.EsSistema,
            r.EsActivo, grants.Where(x => x.RolId == r.RolId).Select(x => x.PermisoId).ToArray())).ToArray();
    }

    public Task<PermissionSummary[]> ListPermissions(CancellationToken ct) =>
        db.Permisos.AsNoTracking().OrderBy(x => x.Codigo)
            .Select(x => new PermissionSummary(x.PermisoId, x.Codigo, x.Modulo, x.Accion, x.Descripcion))
            .ToArrayAsync(ct);

    public async Task SetUserState(long id, string code, long actorId, CancellationToken ct)
    {
        if (id == actorId && code != "ACTIVO")
            throw new SecurityFault(409, "No puede bloquear su propia cuenta.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await FindById(id, ct) ?? throw new SecurityFault(404, "Usuario no encontrado.");
        var state = await StateId(code, ct);
        if (code != "ACTIVO") await GuardLastAdmin(id, ct);
        user.EstadoId = state;
        user.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (code != "ACTIVO") await RevokeSessions(id, ct);
        await tx.CommitAsync(ct);
    }

    public async Task SetUserRoles(long id, int[] roleIds, long actorId, CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToArray();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        _ = await FindById(id, ct) ?? throw new SecurityFault(404, "Usuario no encontrado.");
        if (await db.Roles.CountAsync(x => ids.Contains(x.RolId) && x.EsActivo, ct) != ids.Length)
            throw new SecurityFault(400, "Uno o mas roles no existen o estan inactivos.");
        var adminId = await db.Roles.Where(x => x.Codigo == "ADMINISTRADOR").Select(x => x.RolId).SingleAsync(ct);
        if (!ids.Contains(adminId))
        {
            if (id == actorId) throw new SecurityFault(409, "No puede retirarse su rol de administrador.");
            await GuardLastAdmin(id, ct);
        }
        var current = await db.UsuarioRoles.Where(x => x.UsuarioId == id).ToArrayAsync(ct);
        db.UsuarioRoles.RemoveRange(current.Where(x => !ids.Contains(x.RolId)));
        db.UsuarioRoles.AddRange(ids.Except(current.Select(x => x.RolId)).Select(roleId => new UsuarioRol
        {
            UsuarioId = id, RolId = roleId, AsignadoPorUsuarioId = actorId, AsignadoEn = DateTime.UtcNow
        }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task GuardLastAdmin(long id, CancellationToken ct)
    {
        var activeAdmins = await (from ur in db.UsuarioRoles
            join r in db.Roles on ur.RolId equals r.RolId
            join u in db.Usuarios on ur.UsuarioId equals u.UsuarioId
            join e in db.Estados on u.EstadoId equals e.EstadoId
            where r.Codigo == "ADMINISTRADOR" && r.EsActivo && e.Grupo == "USUARIO"
                && e.Codigo == "ACTIVO" && e.EsActivo select u.UsuarioId).Distinct().ToArrayAsync(ct);
        if (activeAdmins.Length == 1 && activeAdmins[0] == id)
            throw new SecurityFault(409, "Debe conservarse al menos un administrador activo.");
    }

    private Task<int> RevokeSessions(long id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.Sesiones.Where(x => x.UsuarioId == id && x.FinalizadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.FinalizadaEn, now), ct);
    }

    public async Task<int> CreateRole(CreateRoleRequest request, CancellationToken ct)
    {
        var role = new Rol { Codigo = request.Codigo, Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(), EsSistema = false, EsActivo = true };
        if (role.Nombre.Length == 0) throw new SecurityFault(400, "El nombre es obligatorio.");
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role.RolId;
    }

    public async Task<int> CreatePermission(CreatePermissionRequest request, CancellationToken ct)
    {
        var permission = new Permiso { Codigo = request.Codigo, Modulo = request.Modulo.Trim(),
            Accion = request.Accion.Trim(), Descripcion = request.Descripcion?.Trim() };
        if (permission.Modulo.Length == 0 || permission.Accion.Length == 0)
            throw new SecurityFault(400, "Modulo y accion son obligatorios.");
        db.Permisos.Add(permission);
        await db.SaveChangesAsync(ct);
        return permission.PermisoId;
    }

    public async Task SetRolePermissions(int id, int[] permissionIds, CancellationToken ct)
    {
        var ids = permissionIds.Distinct().ToArray();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.RolId == id, ct)
            ?? throw new SecurityFault(404, "Rol no encontrado.");
        if (await db.Permisos.CountAsync(x => ids.Contains(x.PermisoId), ct) != ids.Length)
            throw new SecurityFault(400, "Uno o mas permisos no existen.");
        if (role.Codigo == "ADMINISTRADOR")
        {
            var critical = await db.Permisos.Where(x => x.Codigo == "SEGURIDAD_USUARIOS_GESTIONAR"
                || x.Codigo == "SEGURIDAD_USUARIOS_LEER").Select(x => x.PermisoId).ToArrayAsync(ct);
            if (critical.Length != 2 || critical.Except(ids).Any())
                throw new SecurityFault(409, "El administrador debe conservar los permisos de seguridad.");
        }
        var current = await db.RolPermisos.Where(x => x.RolId == id).ToArrayAsync(ct);
        db.RolPermisos.RemoveRange(current.Where(x => !ids.Contains(x.PermisoId)));
        db.RolPermisos.AddRange(ids.Except(current.Select(x => x.PermisoId))
            .Select(pid => new RolPermiso { RolId = id, PermisoId = pid }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<bool> CanConnect(CancellationToken ct) =>
        await db.Roles.AnyAsync(x => x.Codigo == "CLIENTE" && x.EsActivo, ct)
        && await db.Estados.AnyAsync(x => x.Grupo == "USUARIO" && x.Codigo == "ACTIVO" && x.EsActivo, ct);
}

