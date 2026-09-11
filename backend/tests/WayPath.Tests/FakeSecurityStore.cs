using WayPath.Application;
using WayPath.Domain.Seguridad;
using WayPath.Infrastructure;

namespace WayPath.Tests;

// Isolated test double. No test in this suite opens or writes the user's SQL Server database.
public sealed class FakeSecurityStore : ISecurityStore
{
    public const string TestPassword = "Test-only.password.42";
    public Dictionary<long, Usuario> Users { get; } = [];
    public Dictionary<long, string[]> Roles { get; } = [];
    public Dictionary<long, string[]> Permissions { get; } = [];
    public Dictionary<string, long> Sessions { get; } = [];
    private readonly object sync = new();
    private long nextId = 3;

    public FakeSecurityStore()
    {
        var hasher = new PasswordService();
        foreach (var id in new long[] { 1, 2 })
        {
            var user = new Usuario { UsuarioId = id, Email = id == 1 ? "cliente@example.test" : "admin@example.test",
                NombreUsuario = "Prueba", EstadoId = 1 };
            user.PasswordHash = hasher.Hash(user, TestPassword);
            Users[id] = user;
        }
        Roles[1] = ["CLIENTE"];
        Roles[2] = ["ADMINISTRADOR"];
        Permissions[1] = ["CATALOGO_DESTINOS_LEER"];
        Permissions[2] = ["SEGURIDAD_USUARIOS_GESTIONAR", "SEGURIDAD_USUARIOS_LEER"];
    }

    public Task<Usuario?> FindByEmail(string email, CancellationToken ct) =>
        Task.FromResult(Users.Values.SingleOrDefault(x => x.Email == email));
    public Task<Usuario?> FindById(long id, CancellationToken ct) => Task.FromResult(Users.GetValueOrDefault(id));
    public Task<bool> IsActive(Usuario user, CancellationToken ct) => Task.FromResult(user.EstadoId == 1);
    private UserAccess Access(long id) => new(id.ToString(), Users[id].Email, Users[id].NombreUsuario, Roles[id], Permissions[id]);
    public Task<UserAccess?> GetAccess(long id, string sessionId, CancellationToken ct) =>
        Task.FromResult<UserAccess?>(Sessions.GetValueOrDefault(sessionId) == id &&
            Users.TryGetValue(id, out var user) && user.EstadoId == 1 ? Access(id) : null);
    public Task<long> CreateUser(Usuario user, bool firstAdministrator, CancellationToken ct)
    {
        lock (sync)
        {
            if (Users.Values.Any(x => x.Email == user.Email))
                throw new SecurityFault(409, "Cuenta existente.");
            if (firstAdministrator && Roles.Values.Any(x => x.Contains("ADMINISTRADOR")))
                throw new SecurityFault(409, "Administrador existente.");
            user.UsuarioId = nextId++;
            user.EstadoId = 1;
            Users[user.UsuarioId] = user;
            Roles[user.UsuarioId] = [firstAdministrator ? "ADMINISTRADOR" : "CLIENTE"];
            Permissions[user.UsuarioId] = [];
            return Task.FromResult(user.UsuarioId);
        }
    }
    public Task<LoginSession> StartSession(Usuario user, string? ip, string? agent, CancellationToken ct)
    {
        var sid = Guid.NewGuid().ToString("N");
        Sessions[sid] = user.UsuarioId;
        return Task.FromResult(new LoginSession(Access(user.UsuarioId), sid));
    }
    public Task EndSession(long id, string sessionId, CancellationToken ct)
    {
        Sessions.Remove(sessionId);
        return Task.CompletedTask;
    }
    public Task UpdateProfile(long id, string name, CancellationToken ct)
    {
        Users[id].NombreUsuario = name;
        return Task.CompletedTask;
    }
    public Task ChangePassword(Usuario user, string hash, CancellationToken ct)
    {
        user.PasswordHash = hash;
        foreach (var sid in Sessions.Where(x => x.Value == user.UsuarioId).Select(x => x.Key).ToArray())
            Sessions.Remove(sid);
        return Task.CompletedTask;
    }
    public Task<UserPage> ListUsers(int page, int pageSize, CancellationToken ct) =>
        Task.FromResult(new UserPage(Users.Values.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserSummary(u.UsuarioId.ToString(), u.Email, u.NombreUsuario, "ACTIVO", false, "")).ToArray(),
            page, pageSize, Users.Count));
    public Task<RoleSummary[]> ListRoles(CancellationToken ct) => Task.FromResult<RoleSummary[]>([]);
    public Task<PermissionSummary[]> ListPermissions(CancellationToken ct) => Task.FromResult<PermissionSummary[]>([]);
    public Task SetUserState(long id, string code, long actorId, CancellationToken ct) => throw new NotSupportedException();
    public Task SetUserRoles(long id, int[] roleIds, long actorId, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> CreateRole(CreateRoleRequest request, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> CreatePermission(CreatePermissionRequest request, CancellationToken ct) => throw new NotSupportedException();
    public Task SetRolePermissions(int id, int[] permissionIds, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> CanConnect(CancellationToken ct) => Task.FromResult(true);
}

