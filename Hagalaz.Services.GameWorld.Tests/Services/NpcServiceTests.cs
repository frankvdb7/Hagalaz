using AutoMapper;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Services;

[TestClass]
public sealed class NpcServiceTests
{
    [TestMethod]
    public async Task UnregisterDestroyedStaleFamiliar_DoesNotChangeActiveFamiliar()
    {
        var familiarA = Substitute.For<INpc>();
        var familiarB = Substitute.For<INpc>();
        var familiarScriptA = Substitute.For<IFamiliarScript>();
        var familiarScriptB = Substitute.For<IFamiliarScript>();
        var character = Substitute.For<ICharacter>();
        character.FamiliarId.Returns(6816);
        character.FamiliarScript.Returns(familiarScriptB);
        familiarScriptA.Summoner.Returns(character);
        familiarScriptA.Familiar.Returns(familiarA);
        familiarScriptB.Familiar.Returns(familiarB);

        familiarA.IsDestroyed.Returns(true);
        familiarA.Script.Returns(familiarScriptA);

        var npcStore = new SuccessfulNpcStore();
        var npcService = CreateNpcService(npcStore);

        Assert.IsTrue(character.HasFamiliar());

        await npcService.UnregisterAsync(familiarA);

        Assert.AreSame(familiarScriptB, character.FamiliarScript);
        Assert.AreEqual(6816, character.FamiliarId);
        Assert.IsTrue(character.HasFamiliar());
        character.DidNotReceive().DetachFamiliar(Arg.Any<INpc>());
    }

    private static NpcService CreateNpcService(INpcStore npcStore)
    {
        var npcDefinitionStore = new NpcDefinitionStore(
            Substitute.For<IServiceProvider>(),
            Substitute.For<ITypeProvider<INpcType>>(),
            Substitute.For<IMapper>(),
            Substitute.For<ILogger<NpcDefinitionStore>>());

        return new NpcService(
            npcStore,
            npcDefinitionStore,
            Substitute.For<ITypeProvider<INpcDefinition>>(),
            Substitute.For<ILogger<NpcService>>());
    }

    private sealed class SuccessfulNpcStore : INpcStore
    {
        public IAsyncEnumerable<INpc> FindAllAsync() => throw new NotSupportedException();

        public ValueTask<int> CountAsync() => throw new NotSupportedException();

        public ValueTask<bool> AddAsync(INpc npc) => throw new NotSupportedException();

        public ValueTask<bool> RemoveAsync(INpc npc) => new(true);

        public ValueTask<INpc?> FindAsync(Func<INpc, bool> predicate) => throw new NotSupportedException();
    }
}
