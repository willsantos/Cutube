using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cutube.Api.Tests.Helpers;

/// <summary>
/// Factory for creating a test instance of the API application
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Services can be replaced with mocks here for testing
            // Example: Replace real download service with a mock for faster tests
        });
    }
}
