using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Services;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class GameObjectDefinitionOverrideIsolationTests
{
    [TestMethod]
    public async Task FindDefinitionsByIds_AppliesDatabaseOverridesToFreshProviderResults()
    {
        var repository = Substitute.For<IGameObjectDefinitionRepository>();
        repository.FindOverridesByIdsAsync(Arg.Any<IReadOnlyCollection<uint>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new Dictionary<uint, GameObjectDefinitionOverride>
            {
                [100] = new GameObjectDefinitionOverride(100, "database", 42)
            }));

        var providerResults = new Queue<GameObjectDefinition>(
        [
            new GameObjectDefinition(100) { Examine = "archive", LootTableId = 7 },
            new GameObjectDefinition(100) { Examine = "archive", LootTableId = 7 }
        ]);
        var provider = Substitute.For<ITypeProvider<GameObjectDefinition>>();
        provider.Get(100).Returns(_ => providerResults.Dequeue());
        var service = new GameObjectService(Substitute.For<IMapRegionService>(), repository, provider);

        var firstResult = await service.FindGameObjectDefinitionsByIdsAsync([100]);
        var first = firstResult[100];
        Assert.AreEqual("database", first.Examine);
        Assert.AreEqual(42, first.LootTableId);

        first.Examine = "caller mutation";
        var secondResult = await service.FindGameObjectDefinitionsByIdsAsync([100]);
        var second = secondResult[100];

        Assert.AreNotSame(first, second);
        Assert.AreEqual("database", second.Examine);
        Assert.AreEqual(42, second.LootTableId);
        provider.Received(2).Get(100);
    }
}
