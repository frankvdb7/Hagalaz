using System;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Configuration;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldOptionsValidatorTests
{
    [TestMethod]
    public void Validate_MissingIdentity_FailsWithoutDefaultingToWorldOne()
    {
        var options = CreateValidOptions();
        options.Id = 0;

        var result = new WorldOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.FailureMessage, "World.Id");
    }

    [TestMethod]
    public void Validate_InvalidAdvertisedEndpointFails()
    {
        var options = CreateValidOptions();
        options.AdvertisedEndpoint.Port = 65536;

        var result = new WorldOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.FailureMessage, "AdvertisedEndpoint.Port");
    }

    [TestMethod]
    public void Validate_InvalidListenHostFails()
    {
        var options = CreateValidOptions();
        options.ListenHost = "not-an-ip-address";

        var result = new WorldOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.FailureMessage, "ListenHost");
    }

    private static WorldOptions CreateValidOptions() => new()
    {
        Id = 1,
        Name = "World 1",
        AdvertisedEndpoint = new WorldEndpointOptions { Host = "127.0.0.1", Port = 43594 },
        RegistrationLeaseDuration = TimeSpan.FromSeconds(30),
        RegistrationRenewalInterval = TimeSpan.FromSeconds(10),
        RegistrationRetryDelay = TimeSpan.FromSeconds(1)
    };
}
