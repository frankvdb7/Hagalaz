using Hagalaz.Cache;
using Hagalaz.Cache.Extensions;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class CacheEndpointsAuthorizationTests
{
    [TestMethod]
    public void MapCacheEndpoints_AppliesCacheAdminPolicyToGroup()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.Configure<CacheOptions>(options => options.Path = "./Cache");
        builder.Services.AddGameCache();

        var app = builder.Build();
        app.MapCacheEndpoints();

        var cacheEndpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is not null && endpoint.RoutePattern.RawText.Contains("api/v1/cache"))
            .ToArray();

        Assert.IsNotEmpty(cacheEndpoints);
        Assert.IsTrue(cacheEndpoints.All(endpoint =>
        {
            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            return authorizeData.Any(data => data.Policy == CacheAuthorization.PolicyName);
        }));
    }
}
