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
    public async Task UnregisterFamiliar_ClearsSummonerStateAndAllowsResummon()
    {
        var familiarId = 6815;
        var familiarScript = Substitute.For<IFamiliarScript>();
        var character = Substitute.For<ICharacter>();
        character.FamiliarId.Returns(_ => familiarId);
        character.FamiliarScript.Returns(_ => familiarId == 0 ? null! : familiarScript);
        character.When(character => character.DetachFamiliar()).Do(_ => familiarId = 0);
        familiarScript.Summoner.Returns(character);

        var npc = Substitute.For<INpc>();
        npc.IsDestroyed.Returns(false);
        npc.Script.Returns(familiarScript);

        var npcStore = new SuccessfulNpcStore();
        var npcService = CreateNpcService(npcStore);

        Assert.IsTrue(character.HasFamiliar());

        await npcService.UnregisterAsync(npc);

        Assert.IsFalse(character.HasFamiliar());
        character.Received(1).DetachFamiliar();

        familiarId = 6816;
        Assert.IsTrue(character.HasFamiliar());
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
