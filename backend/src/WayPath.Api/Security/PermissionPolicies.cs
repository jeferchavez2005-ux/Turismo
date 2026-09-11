using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WayPath.Api.Security;

// New modules can declare [Authorize(Policy = "permission:CODIGO")] without hardcoded role lists.
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        const string prefix = "permission:";
        if (!policyName.StartsWith(prefix, StringComparison.Ordinal))
            return base.GetPolicyAsync(policyName);
        return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().RequireClaim("permission", policyName[prefix.Length..]).Build());
    }
}

