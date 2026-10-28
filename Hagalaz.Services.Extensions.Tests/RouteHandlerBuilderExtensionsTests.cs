using System.Net;
using Hagalaz.Services.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Extensions.Tests;

[TestClass]
public sealed class RouteHandlerBuilderExtensionsTests
{
    [TestMethod]
    public async Task WithCacheControl_DefaultsToPrivatePolicy()
    {
        await using var app = await StartAsync(endpoints =>
            endpoints.MapGet("/private", () => TypedResults.Ok())
                .WithCacheControl(TimeSpan.FromMinutes(5)));
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/private");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var cacheControl = response.Headers.CacheControl;
        Assert.IsNotNull(cacheControl);
        Assert.IsTrue(cacheControl.Private);
        Assert.IsFalse(cacheControl.Public);
        Assert.AreEqual(TimeSpan.FromMinutes(5), cacheControl.MaxAge);
    }

    [TestMethod]
    public async Task WithCacheControl_AppliesPublicPolicyToPartialAndNotModifiedResponses()
    {
        await using var app = await StartAsync(endpoints =>
        {
            endpoints.MapGet("/partial", () => TypedResults.StatusCode(StatusCodes.Status206PartialContent))
                .WithCacheControl(TimeSpan.FromHours(1), isPublic: true);
            endpoints.MapGet("/not-modified", () => TypedResults.StatusCode(StatusCodes.Status304NotModified))
                .WithCacheControl(TimeSpan.FromHours(1), isPublic: true);
        });
        using var client = CreateClient(app);

        using var partialResponse = await client.GetAsync("/partial");
        Assert.AreEqual(HttpStatusCode.PartialContent, partialResponse.StatusCode);
        var partialCacheControl = partialResponse.Headers.CacheControl;
        Assert.IsNotNull(partialCacheControl);
        Assert.IsTrue(partialCacheControl.Public);
        Assert.AreEqual(TimeSpan.FromHours(1), partialCacheControl.MaxAge);

        using var notModifiedResponse = await client.GetAsync("/not-modified");
        Assert.AreEqual(HttpStatusCode.NotModified, notModifiedResponse.StatusCode);
        var notModifiedCacheControl = notModifiedResponse.Headers.CacheControl;
        Assert.IsNotNull(notModifiedCacheControl);
        Assert.IsTrue(notModifiedCacheControl.Public);
        Assert.AreEqual(TimeSpan.FromHours(1), notModifiedCacheControl.MaxAge);
    }

    [TestMethod]
    public async Task WithCacheControl_PreservesExplicitCachePolicy()
    {
        await using var app = await StartAsync(endpoints =>
            endpoints.MapGet("/explicit", (HttpContext context) =>
                {
                    context.Response.Headers["Cache-Control"] = "no-store";
                    return TypedResults.Ok();
                })
                .WithCacheControl(TimeSpan.FromDays(1), isPublic: true));
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/explicit");

        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
    }

    [TestMethod]
    public async Task WithCacheControl_SkipsResponsesThatSetCookies()
    {
        await using var app = await StartAsync(endpoints =>
            endpoints.MapGet("/cookie", (HttpContext context) =>
                {
                    context.Response.Cookies.Append("session", "test");
                    return TypedResults.Ok();
                })
                .WithCacheControl(TimeSpan.FromDays(1), isPublic: true));
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/cookie");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains("Set-Cookie"));
        Assert.IsNull(response.Headers.CacheControl);
    }

    [TestMethod]
    public async Task WithCacheControl_SkipsNonCacheableStatuses()
    {
        await using var app = await StartAsync(endpoints =>
            endpoints.MapGet("/missing", () => TypedResults.NotFound())
                .WithCacheControl(TimeSpan.FromDays(1), isPublic: true));
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/missing");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.IsNull(response.Headers.CacheControl);
    }

    [TestMethod]
    public async Task WithCacheControl_AcceptsZeroMaxAge()
    {
        await using var app = await StartAsync(endpoints =>
            endpoints.MapGet("/zero", () => TypedResults.Ok())
                .WithCacheControl(TimeSpan.Zero));
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/zero");

        Assert.IsNotNull(response.Headers.CacheControl);
        Assert.AreEqual(TimeSpan.Zero, response.Headers.CacheControl.MaxAge);
    }

    [TestMethod]
    public void WithCacheControl_RejectsNullBuilder()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => RouteHandlerBuilderExtensions.WithCacheControl(null!, TimeSpan.Zero));
    }

    [TestMethod]
    public void WithCacheControl_RejectsNegativeMaxAge()
    {
        using var app = WebApplication.CreateBuilder().Build();
        var endpoint = app.MapGet("/negative", () => TypedResults.Ok());

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => endpoint.WithCacheControl(TimeSpan.FromTicks(-1)));
    }

    private static async Task<WebApplication> StartAsync(Action<IEndpointRouteBuilder> mapEndpoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var app = builder.Build();
        mapEndpoints(app);
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }
}