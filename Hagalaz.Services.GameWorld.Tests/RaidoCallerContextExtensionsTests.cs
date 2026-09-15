using System.Collections.Generic;
using Hagalaz.Services.GameWorld.Extensions;
using Hagalaz.Services.GameWorld.Features;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Raido.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class RaidoCallerContextExtensionsTests
{
    [TestMethod]
    public void GetMasterId_WhenSubjectIsMalformed_ReturnsNull()
    {
        var context = CreateContext(new Dictionary<string, object> { [Claims.Subject] = "not-a-number" });

        Assert.IsNull(context.GetMasterId());
    }

    [TestMethod]
    public void GetMasterId_WhenSubjectIsNumeric_ReturnsMasterId()
    {
        var context = CreateContext(new Dictionary<string, object> { [Claims.Subject] = "42" });

        Assert.AreEqual(42u, context.GetMasterId());
    }

    private static RaidoCallerContext CreateContext(IDictionary<string, object> claims)
    {
        var context = Substitute.For<RaidoCallerContext>();
        var features = new FeatureCollection();
        features.Set<IAuthenticationFeature>(new AuthenticationFeature
        {
            AuthenticationProperties = new AuthenticationProperties { Claims = claims }
        });
        context.Features.Returns(features);
        return context;
    }
}
