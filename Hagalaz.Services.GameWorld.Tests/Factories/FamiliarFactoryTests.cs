using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Factories;

[TestClass]
public sealed class FamiliarFactoryTests
{
    [TestMethod]
    public void Spawn_ComposesOwnerAwareScriptAndAttachesItToSummoner()
    {
        var builder = Substitute.For<INpcBuilder>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();
        var handle = Substitute.For<INpcHandle>();
        var summoner = Substitute.For<ICharacter>();
        var location = Substitute.For<ILocation>();
        var owner = Substitute.For<INpc>();
        var definition = new SummoningDto { NpcId = 6815 };
        var script = Substitute.For<IFamiliarScript>();
        var activator = Substitute.For<INpcScriptActivator>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        Func<INpcScriptActivator, INpc, INpcScript>? scriptFactory = null;
        var familiar = Substitute.For<INpc>();

        summoner.Location.Returns(location);
        script.Familiar.Returns(familiar);
        builder.Create().Returns(npcId);
        npcId.WithId(definition.NpcId).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Do<Func<INpcScriptActivator, INpc, INpcScript>>(factory => scriptFactory = factory))
            .Returns(optional);
        optional.Spawn().Returns(_ =>
        {
            var createdScript = scriptFactory!(activator, owner);
            Assert.AreSame(script, createdScript);
            return handle;
        });
        scriptProvider.FindFamiliarScriptTypeById(definition.NpcId).Returns(typeof(FamiliarFactoryTests));
        activator.Create(typeof(FamiliarFactoryTests), owner).Returns(script);

        var factory = new FamiliarFactory(
            builder,
            scriptProvider,
            Substitute.For<ISummoningDefinitionStore>(),
            new FamiliarRestorationState());

        var result = factory.Spawn(summoner, definition);

        Assert.AreSame(handle, result);
        script.Received(1).AttachToSummoner(summoner, definition);
        summoner.Received(1).AttachFamiliar(script, definition.NpcId);
    }

    [TestMethod]
    public void TryRestore_AppliesPendingFamiliarStateAfterRegistrationPreservingTicks()
    {
        var builder = Substitute.For<INpcBuilder>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();
        var summoner = Substitute.For<ICharacter>();
        var location = Substitute.For<ILocation>();
        var owner = Substitute.For<INpc>();
        var definition = new SummoningDto { NpcId = 6815 };
        var script = (IFamiliarScript)Substitute.For(
            new[]
            {
                typeof(IFamiliarScript),
                typeof(IHydratable<HydratedFamiliar>),
                typeof(IHydratable<IReadOnlyList<HydratedItem>>),
                typeof(IDehydratable<HydratedFamiliar>)
            }, Array.Empty<object>());
        var activator = Substitute.For<INpcScriptActivator>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        var definitions = Substitute.For<ISummoningDefinitionStore>();
        var restorationState = new FamiliarRestorationState();
        var handle = Substitute.For<INpcHandle>();
        Func<INpcScriptActivator, INpc, INpcScript>? scriptFactory = null;
        var familiarState = new HydratedFamiliarDto
        {
            FamiliarId = definition.NpcId,
            TicksRemaining = 42,
            SpecialMovePoints = 7,
            IsUsingSpecialMove = true
        };
        var inventory = new HydratedItem(1, 2, 3, "data");

        restorationState.SetFamiliar(familiarState);
        restorationState.SetInventory([inventory]);
        summoner.Location.Returns(location);
        builder.Create().Returns(npcId);
        npcId.WithId(definition.NpcId).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Do<Func<INpcScriptActivator, INpc, INpcScript>>(factory => scriptFactory = factory))
            .Returns(optional);
        optional.Spawn().Returns(_ =>
        {
            var registeredScript = scriptFactory!(activator, owner);
            registeredScript.OnSpawn();
            return handle;
        });
        scriptProvider.FindFamiliarScriptTypeById(definition.NpcId).Returns(typeof(FamiliarFactoryTests));
        definitions.FindByNpcId(definition.NpcId).Returns(definition);
        activator.Create(typeof(FamiliarFactoryTests), owner).Returns(script);

        var activeState = new HydratedFamiliar { TicksRemaining = definition.Ticks };
        ((IHydratable<HydratedFamiliar>)script)
            .When(x => x.Hydrate(Arg.Any<HydratedFamiliar>()))
            .Do(callInfo => activeState = callInfo.Arg<HydratedFamiliar>());
        ((IDehydratable<HydratedFamiliar>)script)
            .Dehydrate()
            .Returns(_ => activeState);
        script.When(x => x.OnSpawn())
            .Do(_ => activeState = new HydratedFamiliar { TicksRemaining = definition.Ticks });

        var factory = new FamiliarFactory(builder, scriptProvider, definitions, restorationState);

        Assert.IsTrue(factory.TryRestore(summoner));

        ((IHydratable<HydratedFamiliar>)script).Received(1).Hydrate(Arg.Is<HydratedFamiliar>(state =>
            state.TicksRemaining == familiarState.TicksRemaining &&
            state.SpecialMovePoints == familiarState.SpecialMovePoints &&
            state.IsUsingSpecialMove == familiarState.IsUsingSpecialMove));
        ((IHydratable<IReadOnlyList<HydratedItem>>)script).Received(1).Hydrate(
            Arg.Is<IReadOnlyList<HydratedItem>>(items => items.Count == 1 && items[0] == inventory));
        script.Received(1).OnSpawn();
        Assert.AreEqual(familiarState.TicksRemaining, ((IDehydratable<HydratedFamiliar>)script).Dehydrate().TicksRemaining);
        summoner.Received(1).AttachFamiliar(script, definition.NpcId);
        Assert.IsNull(restorationState.FamiliarId);
        Assert.IsNull(restorationState.Familiar);
        Assert.IsNull(restorationState.Inventory);
    }

}
