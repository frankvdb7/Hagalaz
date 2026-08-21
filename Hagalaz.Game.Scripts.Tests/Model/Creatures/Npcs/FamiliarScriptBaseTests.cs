using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Model.Creatures.Npcs;

[TestClass]
public sealed class FamiliarScriptBaseTests
{
    [TestMethod]
    public void AttachToSummoner_AppliesDefinitionAndRunsFamiliarSetup()
    {
        var summoner = Substitute.For<ICharacter>();
        var script = new TestFamiliarScript(
            Substitute.For<INpc>(),
            Substitute.For<ISmartPathFinder>(),
            Substitute.For<INpcService>(),
            Substitute.For<IItemService>(),
            Substitute.For<IItemBuilder>(),
            Substitute.For<IWidgetScriptActivator>());
        var definition = new SummoningDto { NpcId = 6815 };

        script.AttachToSummoner(summoner, definition);

        Assert.AreSame(summoner, script.Summoner);
        Assert.AreSame(definition, script.Definition);
        Assert.AreEqual(60, script.SpecialMovePoints);
        Assert.IsTrue(script.AttachedSetupRan);
    }

    [TestMethod]
    public void OnDestroy_DetachesTheFamiliarFromItsSummoner()
    {
        var summoner = Substitute.For<ICharacter>();
        var script = new TestFamiliarScript(
            Substitute.For<INpc>(),
            Substitute.For<ISmartPathFinder>(),
            Substitute.For<INpcService>(),
            Substitute.For<IItemService>(),
            Substitute.For<IItemBuilder>(),
            Substitute.For<IWidgetScriptActivator>());

        script.AttachToSummoner(summoner, new SummoningDto { NpcId = 6815 });
        script.OnDestroy();

        summoner.Received(1).DetachFamiliar(script.Familiar);
    }

    [TestMethod]
    public void DismissedFamiliar_DoesNotReceiveCombatTargetEventsAfterNextFamiliarAttaches()
    {
        var summoner = Substitute.For<ICharacter>();
        var npcService = Substitute.For<INpcService>();
        npcService.UnregisterAsync(Arg.Any<INpc>()).Returns(Task.CompletedTask);

        summoner.RegisterEventHandler(Arg.Any<EventHappened<SummoningAllowEvent>>())
            .Returns(Substitute.For<EventHappened>());
        summoner.RegisterEventHandler(Arg.Any<EventHappened<CreatureDiedEvent>>())
            .Returns(Substitute.For<EventHappened>());

        EventHappened<FamiliarDismissEvent>? dismissHandler = null;
        summoner.RegisterEventHandler(
                Arg.Do<EventHappened<FamiliarDismissEvent>>(handler => dismissHandler = handler))
            .Returns(Substitute.For<EventHappened>());

        var activeTargetHandlers = new List<EventHappened<CreatureSetCombatTargetEvent>>();
        summoner.RegisterEventHandler(
                Arg.Do<EventHappened<CreatureSetCombatTargetEvent>>(handler => activeTargetHandlers.Add(handler)))
            .Returns(Substitute.For<EventHappened>());
        summoner.When(x => x.UnregisterEventHandler<CreatureSetCombatTargetEvent>(Arg.Any<EventHappened>()))
            .Do(_ => activeTargetHandlers.RemoveAt(0));

        var familiarA = Substitute.For<INpc>();
        var familiarB = Substitute.For<INpc>();
        var scriptA = CreateScript(familiarA, npcService);
        var scriptB = CreateScript(familiarB, npcService);

        scriptA.AttachToSummoner(summoner, new SummoningDto { NpcId = 6815 });
        Assert.IsNotNull(dismissHandler);
        dismissHandler!(new FamiliarDismissEvent(summoner));
        scriptA.OnDestroy();

        scriptB.AttachToSummoner(summoner, new SummoningDto { NpcId = 6815 });

        Assert.HasCount(1, activeTargetHandlers);
        summoner.Received(1).UnregisterEventHandler<CreatureSetCombatTargetEvent>(Arg.Any<EventHappened>());
        activeTargetHandlers[0](new CreatureSetCombatTargetEvent(summoner, Substitute.For<ICreature>()));

        familiarA.DidNotReceive().QueueTask(Arg.Any<RsTask>());
        familiarB.Received(1).QueueTask(Arg.Any<RsTask>());
    }

    [TestMethod]
    public void OnDestroy_UnregistersAllSummonerHandlers()
    {
        var summoner = Substitute.For<ICharacter>();
        var script = CreateScript(Substitute.For<INpc>(), Substitute.For<INpcService>());

        summoner.RegisterEventHandler(Arg.Any<EventHappened<SummoningAllowEvent>>())
            .Returns(Substitute.For<EventHappened>());
        summoner.RegisterEventHandler(Arg.Any<EventHappened<CreatureDiedEvent>>())
            .Returns(Substitute.For<EventHappened>());
        summoner.RegisterEventHandler(Arg.Any<EventHappened<FamiliarDismissEvent>>())
            .Returns(Substitute.For<EventHappened>());
        summoner.RegisterEventHandler(Arg.Any<EventHappened<CreatureSetCombatTargetEvent>>())
            .Returns(Substitute.For<EventHappened>());

        script.AttachToSummoner(summoner, new SummoningDto { NpcId = 6815 });
        script.OnDestroy();

        summoner.Received(1).UnregisterEventHandler<SummoningAllowEvent>(Arg.Any<EventHappened>());
        summoner.Received(1).UnregisterEventHandler<CreatureDiedEvent>(Arg.Any<EventHappened>());
        summoner.Received(1).UnregisterEventHandler<FamiliarDismissEvent>(Arg.Any<EventHappened>());
        summoner.Received(1).UnregisterEventHandler<CreatureSetCombatTargetEvent>(Arg.Any<EventHappened>());
    }

    private static TestFamiliarScript CreateScript(INpc owner, INpcService? npcService = null) => new(
        owner,
        Substitute.For<ISmartPathFinder>(),
        npcService ?? Substitute.For<INpcService>(),
        Substitute.For<IItemService>(),
        Substitute.For<IItemBuilder>(),
        Substitute.For<IWidgetScriptActivator>());

    private sealed class TestFamiliarScript(
        INpc owner,
        ISmartPathFinder pathFinder,
        INpcService npcService,
        IItemService itemService,
        IItemBuilder itemBuilder,
        IWidgetScriptActivator widgetScriptActivator)
        : FamiliarScriptBase(owner, pathFinder, npcService, itemService, itemBuilder, widgetScriptActivator)
    {
        public bool AttachedSetupRan { get; private set; }

        protected override void OnAttachedToSummoner() => AttachedSetupRan = true;
    }
}
