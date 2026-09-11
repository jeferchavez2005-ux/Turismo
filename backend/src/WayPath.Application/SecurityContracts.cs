using System.ComponentModel.DataAnnotations;
using WayPath.Domain.Seguridad;

namespace WayPath.Application;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(255)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password,
    [Required, StringLength(100)] string Nombre);
public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(255)] string Email,
    [Required, StringLength(128)] string Password);
public sealed record ProfileRequest([Required, StringLength(100)] string Nombre);
public sealed record PasswordRequest(
    [Required, StringLength(128)] string PasswordActual,
    [Required, StringLength(128, MinimumLength = 12)] string PasswordNueva);
public sealed record StateRequest([Required] string Codigo);
public sealed record UserRolesRequest([Required, MaxLength(100)] int[] RolIds);
public sealed record RolePermissionsRequest([Required, MaxLength(500)] int[] PermisoIds);
public sealed record CreateRoleRequest(
    [Required, RegularExpression(@"^[A-Z][A-Z0-9_]{2,49}$")] string Codigo,
    [Required, StringLength(100)] string Nombre,
    [StringLength(250)] string? Descripcion);
public sealed record CreatePermissionRequest(
    [Required, RegularExpression(@"^[A-Z][A-Z0-9_]{2,99}$")] string Codigo,
    [Required, StringLength(80)] string Modulo,
    [Required, StringLength(80)] string Accion,
    [StringLength(250)] string? Descripcion);

public sealed record UserAccess(string UsuarioId, string Email, string? Nombre,
    string[] Roles, string[] Permisos);
public sealed record UserSummary(string UsuarioId, string Email, string? Nombre,
    string Estado, bool EmailConfirmado, string Version);
public sealed record RoleSummary(int RolId, string Codigo, string Nombre, bool EsSistema,
    bool EsActivo, int[] PermisoIds);
public sealed record PermissionSummary(int PermisoId, string Codigo, string Modulo,
    string Accion, string? Descripcion);
public sealed record UserPage(UserSummary[] Items, int Page, int PageSize, int Total);
public sealed record LoginSession(UserAccess User, string SessionId);
public sealed class SecurityFault(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public interface IPasswordService
{
    string Hash(Usuario user, string password);
    bool Verify(Usuario user, string password);
    void DummyVerify(string password);
}

public interface ISecurityStore
{
    Task<Usuario?> FindByEmail(string email, CancellationToken ct);
    Task<Usuario?> FindById(long id, CancellationToken ct);
    Task<bool> IsActive(Usuario user, CancellationToken ct);
    Task<UserAccess?> GetAccess(long id, string sessionId, CancellationToken ct);
    Task<long> CreateUser(Usuario user, bool firstAdministrator, CancellationToken ct);
    Task<LoginSession> StartSession(Usuario user, string? ip, string? agent, CancellationToken ct);
    Task EndSession(long id, string sessionId, CancellationToken ct);
    Task UpdateProfile(long id, string name, CancellationToken ct);
    Task ChangePassword(Usuario user, string hash, CancellationToken ct);
    Task<UserPage> ListUsers(int page, int pageSize, CancellationToken ct);
    Task<RoleSummary[]> ListRoles(CancellationToken ct);
    Task<PermissionSummary[]> ListPermissions(CancellationToken ct);
    Task SetUserState(long id, string code, long actorId, CancellationToken ct);
    Task SetUserRoles(long id, int[] roleIds, long actorId, CancellationToken ct);
    Task<int> CreateRole(CreateRoleRequest request, CancellationToken ct);
    Task<int> CreatePermission(CreatePermissionRequest request, CancellationToken ct);
    Task SetRolePermissions(int id, int[] permissionIds, CancellationToken ct);
    Task<bool> CanConnect(CancellationToken ct);
}

