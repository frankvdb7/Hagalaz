using AutoMapper;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldInfoServiceTests
{
    [TestMethod]
    public async Task WorldInfoService_PreservesConfiguredEndpointAndAvailability()
    {
        var service = new WorldInfoService(
            new WorldInfoStore(),
            new MapperConfiguration(config => config.AddProfile<WorldProfile>(), LoggerFactory.Create(_ => { })).CreateMapper(),
            Substitute.For<HybridCache>());

        await service.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 443));
        var initial = await service.FindAllWorldInfoAsync();
        Assert.AreEqual(443, initial.Single().Port);

        await service.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(1, 10, true));
        var availabilityChanged = await service.FindAllWorldCharacterInfoAsync();
        Assert.IsTrue(availabilityChanged.Single().Online);

        await service.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 444));
        var endpointChanged = await service.FindAllWorldInfoAsync();
        Assert.AreEqual(444, endpointChanged.Single().Port);
    }

    private static WorldInfo CreateWorldInfo(int id, int port) => new()
    {
        Id = id,
        Name = $"World {id}",
        IpAddress = "world.example.test",
        Port = port,
        Location = new WorldLocationInfo("Local", 0),
        Settings = new WorldSettingsInfo
        {
            IsMembersOnly = false,
            IsQuickChatEnabled = true,
            IsPvP = false,
            IsLootShareEnabled = false,
            IsHighLighted = false
        }
    };
}
