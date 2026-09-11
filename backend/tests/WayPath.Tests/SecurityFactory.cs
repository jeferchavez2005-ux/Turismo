using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WayPath.Application;

namespace WayPath.Tests;

public sealed class SecurityFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "VGhpcy1pcy1hLXRlc3Qta2V5LW9ubHktbm90LWZvci1wcm9kdWN0aW9uLTEyMzQ=";
    public FakeSecurityStore Store { get; } = new();
    public string EnvironmentName { get; set; } = "Development";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("ConnectionStrings:WayPathDB", "Server=unused;Database=NeverOpened;Integrated Security=True");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISecurityStore>();
            services.AddSingleton<ISecurityStore>(Store);
        });
    }
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("http://localhost")
    });
}
