using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class FamiliarCharacterScriptTests
{
    [TestMethod]
    public void OnRegistered_WhenFamiliarDefinitionIsMissing_ClearsPendingRestoration()
    {
        var character = Substitute.For<ICharacter>();
        var builder = Substitute.For<INpcBuilder>();
        var context = Substitute.For<ICharacterContext>();
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        var summoningService = Substitute.For<ISummoningService>();

        character.PendingFamiliarId.Returns(6815);
        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);
        summoningService.FindDefinitionByNpcIdSync(6815).Returns((SummoningDto?)null);

        new FamiliarCharacterScript(contextAccessor, builder, scriptProvider, summoningService).OnRegistered();

        character.Received(1).ClearPendingFamiliar();
        builder.DidNotReceive().Create();
        scriptProvider.DidNotReceive().FindFamiliarScriptTypeById(Arg.Any<int>());
    }

    [TestMethod]
    public void OnRegistered_RestoresHydratedFamiliarThroughNpcBuilder()
    {
        var character = Substitute.For<ICharacter>();
        var familiar = Substitute.For<IFamiliarScript>();
        var location = Substitute.For<ILocation>();
        var context = Substitute.For<ICharacterContext>();
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        var builder = Substitute.For<INpcBuilder>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        var summoningService = Substitute.For<ISummoningService>();
        var definition = new SummoningDto { NpcId = 6815 };
        var scriptType = typeof(DefaultFamiliarScript);
        Func<INpcScriptActivator, INpc, INpcScript>? scriptFactory = null;

        character.PendingFamiliarId.Returns(6815);
        character.Location.Returns(location);
        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);
        builder.Create().Returns(npcId);
        npcId.WithId(6815).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Do<Func<INpcScriptActivator, INpc, INpcScript>>(factory => scriptFactory = factory)).Returns(optional);
        scriptProvider.FindFamiliarScriptTypeById(6815).Returns(scriptType);
        summoningService.FindDefinitionByNpcIdSync(6815).Returns(definition);

        new FamiliarCharacterScript(contextAccessor, builder, scriptProvider, summoningService).OnRegistered();

        optional.Received(1).WithScript(Arg.Any<Func<INpcScriptActivator, INpc, INpcScript>>());
        optional.Received(1).Spawn();

        var owner = Substitute.For<INpc>();
        var activator = Substitute.For<INpcScriptActivator>();
        activator.Create(scriptType, owner).Returns(familiar);
        scriptFactory!(activator, owner);

        familiar.Received(1).AttachToSummoner(character, definition);
        character.Received(1).AttachFamiliar(familiar);
    }
}
