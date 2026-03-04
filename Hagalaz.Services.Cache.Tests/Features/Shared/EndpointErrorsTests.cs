using Hagalaz.Services.Cache.Features.Shared;
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
}
