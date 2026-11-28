using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using Hagalaz.Game.Scripts.Minigames.DuelArena.GameObjects;
using Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.DuelArena;

[TestClass]
public sealed class DuelResultLifecycleTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void DestroyedParticipant_WithBothCallbacksReached_CompletesTheResultOnce(bool survivorCallbackFirst)
    {
        var duel = CreateDuel(survivorCallbackFirst);

        duel.Bus.SendEvent(new CreatureDestroyedEvent(duel.Loser.Character));

        Assert.AreEqual(2, duel.DestroyedCallbacksReached, "Both snapshotted callbacks must be reached.");
        duel.Survivor.Character.Received(1).Respawn();
        duel.Loser.Character.DidNotReceive().Respawn();
        duel.Survivor.Character.Widgets.Received(1).OpenWidget(1365, 0, duel.Survivor.EndScreen, false);
        duel.Loser.Character.Widgets.DidNotReceive().OpenWidget(1365, 0, Arg.Any<IWidgetScript>(), false);
    }

    [TestMethod]
    public void RepeatedTerminalEntries_AfterVictory_DoNotRerunTerminalSideEffects()
    {
        var duel = CreateDuel(survivorCallbackFirst: true);

        Assert.IsTrue(duel.Survivor.Script.StartVictoryStage(duel.Survivor.Character, duel.Loser.Character));
        Assert.IsFalse(duel.Survivor.Script.StartVictoryStage(duel.Survivor.Character, duel.Loser.Character));
        Assert.IsFalse(duel.Loser.Script.StartVictoryStage(duel.Survivor.Character, duel.Loser.Character));
        duel.Loser.Script.OnKilledBy(duel.Survivor.Character);
        duel.Bus.SendEvent(new CreatureDestroyedEvent(duel.Loser.Character));

        duel.Survivor.Character.Received(1).Respawn();
        duel.Loser.Character.DidNotReceive().Respawn();
        duel.Survivor.Character.Widgets.Received(1).OpenWidget(1365, 0, duel.Survivor.EndScreen, false);
        duel.Loser.Character.Widgets.Received(1).OpenWidget(1365, 0, duel.Loser.EndScreen, false);
    }

    [TestMethod]
    public void ForfeitedDuel_WhenForfeiterIsDestroyedAfterwards_DoesNotRepeatVictory()
    {
        var duel = CreateDuel(survivorCallbackFirst: true);
        ITaskItem? forfeitTask = null;
        duel.Loser.Character.QueueTask(Arg.Do<ITaskItem>(task => forfeitTask = task)).Returns(Substitute.For<IRsTaskHandle>());
        duel.Loser.Character.GetScript<DuelArenaCombatScript>().Returns(duel.Loser.Script);

        new Trapdoor().OnCharacterClickPerform(duel.Loser.Character, GameObjectClickType.Option1Click);
        var task = (RsTask)forfeitTask!;
        task.Tick();
        task.Tick();
        duel.Bus.SendEvent(new CreatureDestroyedEvent(duel.Loser.Character));
        duel.Loser.Script.OnKilledBy(duel.Survivor.Character);

        duel.Survivor.Character.Received(1).Respawn();
        duel.Loser.Character.Received(1).Respawn();
        duel.Survivor.Character.Widgets.Received(1).OpenWidget(1365, 0, duel.Survivor.EndScreen, false);
        duel.Loser.Character.Widgets.Received(1).OpenWidget(1365, 0, duel.Loser.EndScreen, false);
        duel.Loser.Character.Movement.Received(1).Teleport(Arg.Any<ITeleport>());
    }

    private static Duel CreateDuel(bool survivorCallbackFirst)
    {
        var bus = new InMemoryEventBus();
        var reached = new Counter();
        var survivor = CreateParticipant("Survivor", bus, reached);
        var loser = CreateParticipant("Loser", bus, reached);
        var result = new DuelResult();
        var hintIcons = Substitute.For<IHintIconBuilder>();
        var hintIconType = Substitute.For<IHintIconType>();
        hintIcons.Create().Returns(hintIconType);
        foreach (var opponent in new[] { survivor.Character, loser.Character })
        {
            var hintIconTarget = Substitute.For<IHintIconEntityOptional>();
            hintIconTarget.Build().Returns(Substitute.For<IHintIcon>());
            hintIconType.AtEntity(opponent).Returns(hintIconTarget);
        }

        survivor.Script = CreateScript(survivor, loser, result, hintIcons);
        loser.Script = CreateScript(loser, survivor, result, hintIcons);

        // Production registers the initiator's script first; its opponent's destroy callback is then dispatched first.
        var first = survivorCallbackFirst ? survivor : loser;
        var second = survivorCallbackFirst ? loser : survivor;
        first.Script.OnRegistered();
        second.Script.OnRegistered();
        return new Duel(bus, survivor, loser, reached);
    }

    private static DuelArenaCombatScript CreateScript(Participant owner, Participant opponent, DuelResult result, IHintIconBuilder hintIcons)
    {
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(owner.Character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        owner.EndScreen = new DuelEndScreenScript(accessor);
        var mapRegions = Substitute.For<IMapRegionService>();
        mapRegions.IsAccessible(Arg.Any<ILocation>()).Returns(true);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(DuelEndScreenScript)).Returns(owner.EndScreen);
        services.GetService(typeof(IMapRegionService)).Returns(mapRegions);
        owner.Character.ServiceProvider.Returns(services);
        return new DuelArenaCombatScript(accessor, opponent.Character, new DuelRules(_ => { }), null, null, result, hintIcons,
            Substitute.For<ILogger<DuelArenaCombatScript>>());
    }

    private static Participant CreateParticipant(string name, InMemoryEventBus bus, Counter reached)
    {
        var character = Substitute.For<ICharacter>();
        character.DisplayName.Returns(name);
        // Mirrors Creature.RegisterEventHandler: one shared bus, handlers filtered to the registering creature.
        character.RegisterEventHandler(Arg.Any<EventHappened<CreatureDestroyedEvent>>()).Returns(call =>
        {
            var handler = call.Arg<EventHappened<CreatureDestroyedEvent>>();
            return bus.Listen<CreatureDestroyedEvent>(e =>
            {
                if (!ReferenceEquals(e.Target, character))
                {
                    return false;
                }

                reached.Value++;
                return handler(e);
            });
        });
        character.UnregisterEventHandler<CreatureDestroyedEvent>(Arg.Any<EventHappened>())
            .Returns(call => bus.StopListen<CreatureDestroyedEvent>(call.Arg<EventHappened>()));
        return new Participant(character);
    }

    private sealed class Counter
    {
        public int Value { get; set; }
    }

    private sealed class Participant(ICharacter character)
    {
        public ICharacter Character { get; } = character;
        public DuelArenaCombatScript Script { get; set; } = null!;
        public DuelEndScreenScript EndScreen { get; set; } = null!;
    }

    private sealed record Duel(InMemoryEventBus Bus, Participant Survivor, Participant Loser, Counter Reached)
    {
        public int DestroyedCallbacksReached => Reached.Value;
    }
}
