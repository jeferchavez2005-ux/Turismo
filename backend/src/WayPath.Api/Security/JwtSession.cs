using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using WayPath.Application;

namespace WayPath.Api.Security;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "WayPath";
    public string Audience { get; set; } = "WayPath.Web";
    public string SigningKey { get; set; } = "";
    public int LifetimeMinutes { get; set; } = 15;
}

public sealed class JwtSession(JwtSettings settings, IWebHostEnvironment environment)
{
    public const string CookieName = "WayPath.Access";

    public void Write(HttpResponse response, LoginSession session)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(settings.LifetimeMinutes);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
            [new Claim("sub", session.User.UsuarioId), new Claim("jti", session.SessionId)],
            notBefore: now, expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        response.Cookies.Append(CookieName, new JwtSecurityTokenHandler().WriteToken(token),
            Options(expires));
    }

    public void Clear(HttpResponse response) => response.Cookies.Delete(CookieName, Options(null));

    private CookieOptions Options(DateTime? expires) => new()
    {
        HttpOnly = true, Secure = !environment.IsDevelopment(), SameSite = SameSiteMode.Strict,
        Path = "/", Expires = expires, IsEssential = true
    };
}

