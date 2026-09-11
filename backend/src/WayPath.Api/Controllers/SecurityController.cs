using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/security")]
[Authorize(Policy = "security.manage")]
public sealed class SecurityController(ISecurityStore store) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await store.ListUsers(page, pageSize, ct));

    [HttpPut("users/{id:long}/state")]
    public async Task<IActionResult> State(long id, StateRequest request, CancellationToken ct)
    {
        await store.SetUserState(id, request.Codigo, ActorId, ct);
        return NoContent();
    }

    [HttpPut("users/{id:long}/roles")]
    public async Task<IActionResult> UserRoles(long id, UserRolesRequest request, CancellationToken ct)
    {
        await store.SetUserRoles(id, request.RolIds, ActorId, ct);
        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await store.ListRoles(ct));

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request, CancellationToken ct) =>
        StatusCode(201, new { rolId = await store.CreateRole(request, ct) });

    [HttpPut("roles/{id:int}/permissions")]
    public async Task<IActionResult> RolePermissions(int id, RolePermissionsRequest request, CancellationToken ct)
    {
        await store.SetRolePermissions(id, request.PermisoIds, ct);
        return NoContent();
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken ct) => Ok(await store.ListPermissions(ct));

    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission(CreatePermissionRequest request, CancellationToken ct) =>
        StatusCode(201, new { permisoId = await store.CreatePermission(request, ct) });

    private long ActorId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);
}

