using System.Net;
using System.Text.Json;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Extensions;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Security;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class OpenApiDocumentTests
{
    [TestMethod]
    public async Task EveryCacheOperation_DocumentsSummarySuccessAndSharedErrors()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton<IFileStore>(new MemoryFileStore());
        builder.Services.AddGameCache();
        builder.Services.AddOutputCache();
        builder.Services.AddCacheAuthorization();
        builder.Services.AddOpenApi();
        await using var app = builder.Build();
        app.MapCacheEndpoints();
        app.MapOpenApi();
        await app.StartAsync();
        using var client = CacheServiceTestHost.CreateClient(app);

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var operations = document.RootElement.GetProperty("paths").EnumerateObject()
            .Where(path => path.Name.StartsWith("/api/v1/cache/", StringComparison.Ordinal))
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => (Name: $"{operation.Name.ToUpperInvariant()} {path.Name}", Operation: operation.Value)))
            .ToArray();

        Assert.IsNotEmpty(operations);
        foreach (var (name, operation) in operations)
        {
            Assert.IsTrue(operation.TryGetProperty("summary", out var summary) && summary.GetString() is { Length: > 0 }, $"{name} has no summary.");
            var responses = operation.GetProperty("responses");
            var success = responses.EnumerateObject().Where(response => response.Name is "200" or "201").ToArray();
            Assert.HasCount(1, success, $"{name} must document exactly one success response.");
            Assert.IsTrue(success[0].Value.TryGetProperty("content", out _), $"{name} success response has no content type.");
            foreach (var status in new[] { "400", "401", "403", "404", "422", "500" })
                Assert.IsTrue(responses.TryGetProperty(status, out _), $"{name} does not document {status}.");
        }
    }
}
