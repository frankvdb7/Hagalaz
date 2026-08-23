using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class FamiliarCharacterScriptTests
{
    [TestMethod]
    public void RestoredFamiliar_WhenHydrationFails_IsNotActivatedOrAttached()
    {
        var character = Substitute.For<ICharacter>();
        var familiar = Substitute.For<IFamiliarScript, IHydratable<HydratedFamiliar>>();
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
        Action<INpcScript>? configure = null;
        var expectedException = new InvalidOperationException("familiar hydration failed");

        character.Location.Returns(location);
        character.FamiliarScript.Returns(_ => null!);
        ((IHydratable<HydratedFamiliar>)familiar)
            .When(x => x.Hydrate(Arg.Any<HydratedFamiliar>()))
            .Do(_ => throw expectedException);
        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);
        builder.Create().Returns(npcId);
        npcId.WithId(6815).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Any<Type>(), Arg.Do<Action<INpcScript>>(action => configure = action)).Returns(optional);
        scriptProvider.FindFamiliarScriptTypeById(6815).Returns(scriptType);
        summoningService.FindDefinitionByNpcIdSync(6815).Returns(definition);

        var script = new FamiliarCharacterScript(contextAccessor, builder, scriptProvider, summoningService);
        script.Hydrate(new HydratedFamiliarDto { FamiliarId = 6815 });
        script.OnRegistered();

        var actualException = Assert.ThrowsExactly<InvalidOperationException>(() => configure!(familiar));

        Assert.AreSame(expectedException, actualException);
        Assert.IsNull(character.FamiliarScript);
        familiar.DidNotReceive().AttachToSummoner(Arg.Any<ICharacter>(), Arg.Any<SummoningDto>());
        character.DidNotReceive().AttachFamiliar(Arg.Any<IFamiliarScript>());
        character.DidNotReceive().RegisterEventHandler<SummoningAllowEvent>(Arg.Any<EventHappened<SummoningAllowEvent>>());
        character.DidNotReceive().RegisterEventHandler<CreatureDiedEvent>(Arg.Any<EventHappened<CreatureDiedEvent>>());
        character.DidNotReceive().RegisterEventHandler<FamiliarDismissEvent>(Arg.Any<EventHappened<FamiliarDismissEvent>>());
        character.DidNotReceive().RegisterEventHandler<CreatureSetCombatTargetEvent>(Arg.Any<EventHappened<CreatureSetCombatTargetEvent>>());

        script.OnRegistered();

        builder.Received(1).Create();
    }

    [TestMethod]
    public void OnRegistered_WhenFamiliarDefinitionIsMissing_DiscardsHydratedData()
    {
        var character = Substitute.For<ICharacter>();
        var builder = Substitute.For<INpcBuilder>();
        var context = Substitute.For<ICharacterContext>();
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        var summoningService = Substitute.For<ISummoningService>();

        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);

        var script = new FamiliarCharacterScript(contextAccessor, builder, scriptProvider, summoningService);
        script.Hydrate(new HydratedFamiliarDto { FamiliarId = 6815 });
        script.OnRegistered();

        builder.DidNotReceive().Create();
        scriptProvider.DidNotReceive().FindFamiliarScriptTypeById(Arg.Any<int>());

        summoningService.FindDefinitionByNpcIdSync(6815).Returns(new SummoningDto { NpcId = 6815 });
        script.OnRegistered();

        summoningService.Received(1).FindDefinitionByNpcIdSync(6815);
        builder.DidNotReceive().Create();
    }

    [TestMethod]
    public void OnRegistered_RestoresHydratedFamiliarThroughNpcBuilder()
    {
        var character = Substitute.For<ICharacter>();
        var familiar = Substitute.For<IFamiliarScript, IHydratable<HydratedFamiliar>, IHydratable<IReadOnlyList<HydratedItem>>>();
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
        Action<INpcScript>? configure = null;

        character.Location.Returns(location);
        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);
        builder.Create().Returns(npcId);
        npcId.WithId(6815).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Any<Type>(), Arg.Do<Action<INpcScript>>(action => configure = action)).Returns(optional);
        scriptProvider.FindFamiliarScriptTypeById(6815).Returns(scriptType);
        summoningService.FindDefinitionByNpcIdSync(6815).Returns(definition);

        var script = new FamiliarCharacterScript(contextAccessor, builder, scriptProvider, summoningService);
        var restoredState = new HydratedFamiliar
        {
            TicksRemaining = 37,
            SpecialMovePoints = 12,
            IsUsingSpecialMove = true
        };
        var restoredInventory = new[] { new HydratedItem(995, 3, 0, null) };
        script.Hydrate(new HydratedFamiliarDto
        {
            FamiliarId = 6815,
            TicksRemaining = restoredState.TicksRemaining,
            SpecialMovePoints = restoredState.SpecialMovePoints,
            IsUsingSpecialMove = restoredState.IsUsingSpecialMove
        });
        script.Hydrate(restoredInventory);
        script.OnRegistered();

        optional.Received(1).WithScript(scriptType, Arg.Any<Action<INpcScript>>());
        optional.Received(1).Spawn();

        configure!(familiar);

        Received.InOrder(() =>
        {
            ((IHydratable<HydratedFamiliar>)familiar).Hydrate(restoredState);
            ((IHydratable<IReadOnlyList<HydratedItem>>)familiar).Hydrate(restoredInventory);
            familiar.AttachToSummoner(character, definition);
            character.AttachFamiliar(familiar);
        });
    }
}
