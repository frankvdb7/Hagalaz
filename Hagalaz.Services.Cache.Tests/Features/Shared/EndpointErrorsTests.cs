using System.IO;
using System.Net;
using System.Text.Json;
using Hagalaz.Services.Cache.Tests.Features;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            ("unsupported", HttpStatusCode.UnprocessableEntity, "application/problem+json", "Unsupported cache data.", "unsupported format"),
            ("invalid-data", HttpStatusCode.UnprocessableEntity, "application/problem+json", "Unsupported cache data.", "truncated"),
            ("image", HttpStatusCode.UnprocessableEntity, "application/problem+json", "Unsupported cache data.", "unknown image"),
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

    [TestMethod]
    [DataRow("unexpected", LogLevel.Error)]
    [DataRow("unsupported", LogLevel.Warning)]
    [DataRow("not-found", LogLevel.Debug)]
    public async Task CacheExceptionHandler_LogsHandledExceptionsBySeverity(string kind, LogLevel expected)
    {
        var logger = new RecordingLogger();
        Exception exception = kind switch
        {
            "unexpected" => new InvalidOperationException("boom"),
            "unsupported" => new NotSupportedException("unsupported format"),
            _ => new FileNotFoundException("missing")
        };
        var context = CreateHttpContext();
        context.Response.Body = new MemoryStream();

        Assert.IsTrue(await new CacheExceptionHandler(logger).TryHandleAsync(context, exception, CancellationToken.None));

        var entry = logger.Entries.Single();
        Assert.AreEqual(expected, entry.Level);
        Assert.AreSame(exception, entry.Exception);
    }

    private sealed class RecordingLogger : ILogger<CacheExceptionHandler>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception));
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
        "unsupported" => throw new NotSupportedException("unsupported format"),
        "invalid-data" => throw new InvalidDataException("truncated"),
        "image" => throw new SixLabors.ImageSharp.UnknownImageFormatException("unknown image"),
        _ => throw new InvalidOperationException("boom")
    };
}
