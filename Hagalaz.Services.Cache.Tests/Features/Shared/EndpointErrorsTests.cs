using System.IO;
using System.Net;
using System.Text.Json;
using Hagalaz.Services.Cache.Tests.Features;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Shared;

[TestClass]
public class EndpointErrorsTests
{
    [TestMethod]
    public void Validation_CreatesExpectedProblemDetails()
    {
        var details = EndpointErrors.Validation("bad request detail");

        Assert.AreEqual(StatusCodes.Status400BadRequest, details.Status);
        Assert.AreEqual("Request validation failed.", details.Title);
        Assert.AreEqual("bad request detail", details.Detail);
    }

    [TestMethod]
    public async Task CacheExceptionHandler_PreservesProblemDetailsForMappedExceptions()
    {
        using var store = new MemoryFileStore();
        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup =>
            cacheGroup.MapGet("/test-errors/{kind}", (string kind) => ThrowForTest(kind)));
        using var client = CacheServiceTestHost.CreateClient(app);

        var expected = new[]
        {
            ("not-found", HttpStatusCode.NotFound, "application/json", "Cache file not found.", "missing"),
            ("io", HttpStatusCode.BadRequest, "application/json", "Cache operation failed.", "io failure"),
            ("argument", HttpStatusCode.BadRequest, "application/json", "Invalid request.", "invalid"),
            ("unexpected", HttpStatusCode.InternalServerError, "application/problem+json", "Unexpected cache error.", "boom")
        };

        foreach (var (kind, status, contentType, title, detail) in expected)
        {
            using var response = await client.GetAsync($"/api/v1/cache/test-errors/{kind}");
            Assert.AreEqual(status, response.StatusCode);
            Assert.AreEqual(contentType, response.Content.Headers.ContentType?.MediaType);

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            Assert.AreEqual((int)status, document.RootElement.GetProperty("status").GetInt32());
            Assert.AreEqual(title, document.RootElement.GetProperty("title").GetString());
            Assert.AreEqual(detail, document.RootElement.GetProperty("detail").GetString());
        }
    }

    [TestMethod]
    public async Task MapException_FileNotFound_Returns404()
    {
        var result = EndpointErrors.MapException(new FileNotFoundException("missing"));
        var context = CreateHttpContext();

        await result.ExecuteAsync(context);

        Assert.AreEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [TestMethod]
    public async Task MapException_ArgumentException_Returns400()
    {
        var result = EndpointErrors.MapException(new ArgumentException("invalid"));
        var context = CreateHttpContext();

        await result.ExecuteAsync(context);

        Assert.AreEqual(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [TestMethod]
    public async Task MapException_UnexpectedException_Returns500()
    {
        var result = EndpointErrors.MapException(new Exception("boom"));
        var context = CreateHttpContext();

        await result.ExecuteAsync(context);

        Assert.AreEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    private static HttpContext CreateHttpContext()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services
        };
    }

    private static IResult ThrowForTest(string kind) => kind switch
    {
        "not-found" => throw new FileNotFoundException("missing"),
        "io" => throw new IOException("io failure"),
        "argument" => throw new ArgumentException("invalid"),
        _ => throw new InvalidOperationException("boom")
    };
}
