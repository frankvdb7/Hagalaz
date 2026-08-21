using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
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

        summoner.Location.Returns(location);
        builder.Create().Returns(npcId);
        npcId.WithId(definition.NpcId).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Do<Func<INpcScriptActivator, INpc, INpcScript>>(factory => scriptFactory = factory))
            .Returns(optional);
        optional.Spawn().Returns(handle);
        scriptProvider.FindFamiliarScriptTypeById(definition.NpcId).Returns(typeof(FamiliarFactoryTests));
        activator.Create(typeof(FamiliarFactoryTests), owner).Returns(script);

        var factory = new FamiliarFactory(
            builder,
            scriptProvider,
            Substitute.For<ISummoningDefinitionStore>(),
            new FamiliarRestorationState());

        var result = factory.Spawn(summoner, definition);
        var createdScript = scriptFactory!(activator, owner);

        Assert.AreSame(handle, result);
        Assert.AreSame(script, createdScript);
        script.Received(1).AttachToSummoner(summoner, definition);
        summoner.Received(1).AttachFamiliar(script, definition.NpcId);
    }

    [TestMethod]
    public void TryRestore_AppliesPendingFamiliarStateBeforeAttachingIt()
    {
        var builder = Substitute.For<INpcBuilder>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();
        var summoner = Substitute.For<ICharacter>();
        var location = Substitute.For<ILocation>();
        var owner = Substitute.For<INpc>();
        var definition = new SummoningDto { NpcId = 6815 };
        var script = Substitute.For<IFamiliarScript, IHydratable<HydratedFamiliar>, IHydratable<IReadOnlyList<HydratedItem>>>();
        var activator = Substitute.For<INpcScriptActivator>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        var definitions = Substitute.For<ISummoningDefinitionStore>();
        var restorationState = new FamiliarRestorationState();
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
        scriptProvider.FindFamiliarScriptTypeById(definition.NpcId).Returns(typeof(FamiliarFactoryTests));
        definitions.FindByNpcId(definition.NpcId).Returns(definition);
        activator.Create(typeof(FamiliarFactoryTests), owner).Returns(script);

        var factory = new FamiliarFactory(builder, scriptProvider, definitions, restorationState);

        Assert.IsTrue(factory.TryRestore(summoner));
        scriptFactory!(activator, owner);

        ((IHydratable<HydratedFamiliar>)script).Received(1).Hydrate(Arg.Is<HydratedFamiliar>(state =>
            state.TicksRemaining == familiarState.TicksRemaining &&
            state.SpecialMovePoints == familiarState.SpecialMovePoints &&
            state.IsUsingSpecialMove == familiarState.IsUsingSpecialMove));
        ((IHydratable<IReadOnlyList<HydratedItem>>)script).Received(1).Hydrate(
            Arg.Is<IReadOnlyList<HydratedItem>>(items => items.Count == 1 && items[0] == inventory));
        summoner.Received(1).AttachFamiliar(script, definition.NpcId);
        Assert.IsNull(restorationState.FamiliarId);
        Assert.IsNull(restorationState.Familiar);
        Assert.IsNull(restorationState.Inventory);
    }

    [TestMethod]
    public void TryRestore_MissingDefinitionClearsPendingStateBeforeNextSummon()
    {
        var builder = Substitute.For<INpcBuilder>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();
        var handle = Substitute.For<INpcHandle>();
        var summoner = Substitute.For<ICharacter>();
        var location = Substitute.For<ILocation>();
        var owner = Substitute.For<INpc>();
        var script = Substitute.For<IFamiliarScript>();
        var activator = Substitute.For<INpcScriptActivator>();
        var scriptProvider = Substitute.For<IFamiliarScriptProvider>();
        var definitions = Substitute.For<ISummoningDefinitionStore>();
        var restorationState = new FamiliarRestorationState();
        var restoredDefinition = new HydratedFamiliarDto { FamiliarId = 6815 };
        var nextDefinition = new SummoningDto { NpcId = 6816 };
        Func<INpcScriptActivator, INpc, INpcScript>? scriptFactory = null;

        restorationState.SetFamiliar(restoredDefinition);
        restorationState.SetInventory([new HydratedItem(1, 1, 0, null)]);
        definitions.FindByNpcId(restoredDefinition.FamiliarId).Returns((SummoningDto?)null);
        summoner.FamiliarScript.Returns((IFamiliarScript?)null);
        summoner.FamiliarId.Returns(0);
        summoner.Location.Returns(location);
        builder.Create().Returns(npcId);
        npcId.WithId(nextDefinition.NpcId).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(Arg.Do<Func<INpcScriptActivator, INpc, INpcScript>>(factory => scriptFactory = factory))
            .Returns(optional);
        optional.Spawn().Returns(handle);
        scriptProvider.FindFamiliarScriptTypeById(nextDefinition.NpcId).Returns(typeof(FamiliarFactoryTests));
        activator.Create(typeof(FamiliarFactoryTests), owner).Returns(script);

        var factory = new FamiliarFactory(builder, scriptProvider, definitions, restorationState);

        Assert.IsFalse(factory.TryRestore(summoner));
        Assert.IsFalse(summoner.HasFamiliar());
        Assert.IsNull(restorationState.FamiliarId);
        Assert.IsNull(restorationState.Familiar);
        Assert.IsNull(restorationState.Inventory);

        factory.Spawn(summoner, nextDefinition);
        scriptFactory!(activator, owner);

        script.Received(1).AttachToSummoner(summoner, nextDefinition);
        summoner.Received(1).AttachFamiliar(script, nextDefinition.NpcId);
    }
}
