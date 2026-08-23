using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Npcs.Elementals;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Npcs.Elementals;

[TestClass]
public sealed class GlacorEncounterTests
{
    [TestMethod]
    public void GlacyteTargetChange_PropagatesToGlacorWhenGlacorHasNoTarget()
    {
        var glacor = Substitute.For<INpc>();
        var glacorCombat = Substitute.For<ICreatureCombat>();
        var glacyte = Substitute.For<INpc>();
        var glacyteCombat = Substitute.For<ICreatureCombat>();
        var target = Substitute.For<ICreature>();
        var targetHandlers = new List<EventHappened<CreatureSetCombatTargetEvent>>();
        var builder = Substitute.For<INpcBuilder>();
        var glacyteId = Substitute.For<INpcId>();
        var glacyteLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();
        var handle = Substitute.For<INpcHandle>();
        ITaskItem? queuedTask = null;

        glacor.Combat.Returns(glacorCombat);
        glacorCombat.Target.ReturnsForAnyArgs(_ => null);
        glacor.QueueTask(Arg.Do<ITaskItem>(task => queuedTask = task)).Returns(Substitute.For<IRsTaskHandle>());
        glacyte.Combat.Returns(glacyteCombat);
        glacyteCombat.Target.ReturnsForAnyArgs(_ => null);
        handle.Npc.Returns(glacyte);
        builder.Create().Returns(glacyteId);
        glacyteId.WithId(14303).Returns(glacyteLocation);
        glacyteLocation.WithLocation(Arg.Any<ILocation>()).Returns(optional);
        optional.WithScript(Arg.Any<Type>()).Returns(optional);
        optional.Spawn().Returns(handle);
        glacyte.RegisterEventHandler(Arg.Do<EventHappened<CreatureSetCombatTargetEvent>>(handler => targetHandlers.Add(handler)))
            .Returns((EventHappened)(_ => true));
        glacyte.RegisterEventHandler(Arg.Any<EventHappened<CreatureDiedEvent>>())
            .Returns((EventHappened)(_ => true));

        var encounter = new GlacorEncounter(glacor, builder);
        encounter.Begin();
        encounter.SpawnGlacyte(14303, new Location(3200, 3200, 0, 0), typeof(SappingGlacyte));

        targetHandlers[0](new CreatureSetCombatTargetEvent(glacyte, target));
        Assert.IsNotNull(queuedTask);

        queuedTask!.Tick();

        glacorCombat.Received(1).SetTarget(target);
    }

    [TestMethod]
    public void GlacyteDeath_TracksCountAndLastKilledId()
    {
        var glacor = Substitute.For<INpc>();
        var builder = Substitute.For<INpcBuilder>();
        var glacytes = new[] { Substitute.For<INpc>(), Substitute.For<INpc>(), Substitute.For<INpc>() };
        var handles = new[] { Substitute.For<INpcHandle>(), Substitute.For<INpcHandle>(), Substitute.For<INpcHandle>() };
        var ids = new[] { Substitute.For<INpcId>(), Substitute.For<INpcId>(), Substitute.For<INpcId>() };
        var locations = new[] { Substitute.For<INpcLocation>(), Substitute.For<INpcLocation>(), Substitute.For<INpcLocation>() };
        var optionals = new[] { Substitute.For<INpcOptional>(), Substitute.For<INpcOptional>(), Substitute.For<INpcOptional>() };
        var deathHandlers = new List<EventHappened<CreatureDiedEvent>>();

        builder.Create().Returns(ids[0], ids[1], ids[2]);
        for (var i = 0; i < glacytes.Length; i++)
        {
            var id = 14304 - i;
            var glacyte = glacytes[i];
            var handle = handles[i];
            var npcId = ids[i];
            var npcLocation = locations[i];
            var optional = optionals[i];
            var appearance = Substitute.For<INpcAppearance>();

            handle.Npc.Returns(glacyte);
            glacyte.Appearance.Returns(appearance);
            appearance.CompositeID.Returns(id);
            npcId.WithId(id).Returns(npcLocation);
            npcLocation.WithLocation(Arg.Any<ILocation>()).Returns(optional);
            optional.WithScript(Arg.Any<Type>()).Returns(optional);
            optional.Spawn().Returns(handle);
            glacyte.RegisterEventHandler(Arg.Any<EventHappened<CreatureSetCombatTargetEvent>>())
                .Returns((EventHappened)(_ => true));
            glacyte.RegisterEventHandler(Arg.Do<EventHappened<CreatureDiedEvent>>(handler => deathHandlers.Add(handler)))
                .Returns((EventHappened)(_ => true));
        }

        var encounter = new GlacorEncounter(glacor, builder);
        encounter.Begin();
        for (var i = 0; i < glacytes.Length; i++)
        {
            encounter.SpawnGlacyte(14304 - i, new Location(3200 + i, 3200, 0, 0), typeof(SappingGlacyte));
        }

        deathHandlers[0](new CreatureDiedEvent(glacytes[0]));
        deathHandlers[1](new CreatureDiedEvent(glacytes[1]));
        deathHandlers[2](new CreatureDiedEvent(glacytes[2]));

        Assert.AreEqual(3, encounter.GlacyteDeadCount);
        Assert.AreEqual(14302, encounter.LastKilledGlacyteId);
        Assert.IsTrue(encounter.GlacytesDead);
        Assert.AreEqual(0, encounter.TrackedGlacyteCount);
    }

    [TestMethod]
    public void Clear_UnregistersTrackedGlacytesAndHandlers()
    {
        var glacor = Substitute.For<INpc>();
        var builder = Substitute.For<INpcBuilder>();
        var glacyte = Substitute.For<INpc>();
        var handle = Substitute.For<INpcHandle>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();

        handle.Npc.Returns(glacyte);
        builder.Create().Returns(npcId);
        npcId.WithId(14304).Returns(npcLocation);
        npcLocation.WithLocation(Arg.Any<ILocation>()).Returns(optional);
        optional.WithScript(Arg.Any<Type>()).Returns(optional);
        optional.Spawn().Returns(handle);
        glacyte.RegisterEventHandler(Arg.Any<EventHappened<CreatureSetCombatTargetEvent>>())
            .Returns((EventHappened)(_ => true));
        glacyte.RegisterEventHandler(Arg.Any<EventHappened<CreatureDiedEvent>>())
            .Returns((EventHappened)(_ => true));

        var encounter = new GlacorEncounter(glacor, builder);
        encounter.Begin();
        encounter.SpawnGlacyte(14304, new Location(3200, 3200, 0, 0), typeof(SappingGlacyte));

        encounter.Clear();

        handle.Received(1).Unregister();
        glacyte.Received(1).UnregisterEventHandler<CreatureSetCombatTargetEvent>(Arg.Any<EventHappened>());
        glacyte.Received(1).UnregisterEventHandler<CreatureDiedEvent>(Arg.Any<EventHappened>());
        Assert.AreEqual(0, encounter.TrackedGlacyteCount);
        Assert.IsFalse(encounter.GlacytesSpawned);
    }

    [TestMethod]
    public void EnduringGlacyte_UsesTheBoundGlacorForDistance()
    {
        var glacor = Substitute.For<INpc>();
        var glacyte = Substitute.For<INpc>();
        glacor.Location.Returns(new Location(3200, 3200, 0, 0));
        glacyte.Location.Returns(new Location(3200, 3207, 0, 0));

        var script = new EnduringGlacyte(
            glacyte,
            Substitute.For<INpcService>(),
            Substitute.For<ISimplePathFinder>(),
            Substitute.For<IWidgetScriptActivator>());
        script.BindToGlacor(glacor);

        var damage = script.OnIncomingAttack(Substitute.For<ICreature>(), DamageType.Standard, 100, 0);

        Assert.AreEqual(50, damage);
    }

    [TestMethod]
    public void EnduringGlacyte_CannotBeBoundToMoreThanOneGlacor()
    {
        var glacyte = Substitute.For<INpc>();
        var firstGlacor = Substitute.For<INpc>();
        var secondGlacor = Substitute.For<INpc>();
        var script = new EnduringGlacyte(
            glacyte,
            Substitute.For<INpcService>(),
            Substitute.For<ISimplePathFinder>(),
            Substitute.For<IWidgetScriptActivator>());

        script.BindToGlacor(firstGlacor);

        Assert.ThrowsExactly<InvalidOperationException>(() => script.BindToGlacor(secondGlacor));
    }
}
