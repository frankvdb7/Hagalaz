using System.Net;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Extensions;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Cache.Tests.Features;

internal static class CacheServiceTestHost
{
    public static async Task<WebApplication> StartAsync(
        IFileStore store,
        Action<IEndpointRouteBuilder> mapEndpoints,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton<IFileStore>(store);
        builder.Services.AddGameCache();
        configureServices?.Invoke(builder.Services);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<CacheExceptionHandler>();

        var app = builder.Build();
        app.UseExceptionHandler();
        mapEndpoints(app.MapGroup("/api/v1/cache"));
        await app.StartAsync();
        return app;
    }

    public static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses;
        return new HttpClient { BaseAddress = new Uri(addresses.Single()) };
    }
}
