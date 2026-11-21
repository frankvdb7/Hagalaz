using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.GameObjects;
using NSubstitute;
using Armadyl = Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Armadyl;
using Bandos = Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Bandos;
using Saradomin = Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Saradomin;
using Zamorak = Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Zamorak;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

/// <summary>
///     Pins the four Godwars altars: Option 1 restores prayer points (once per timer, never in combat) and
///     Option 2 teleports to the faction's destination. Expectations are the literals of the original altars.
///     Note: Bandos and Zamorak share one destination, and Saradomin shares Armadyl's.
/// </summary>
[TestClass]
public sealed class GodwarsAltarTests
{
    [TestMethod]
    [DataRow("armadyl", typeof(ArmadylAltarPrayedState))]
    [DataRow("bandos", typeof(BandosAltarPrayedState))]
    [DataRow("saradomin", typeof(SaradominAltarPrayedState))]
    [DataRow("zamorak", typeof(ZamorakAltarPrayedState))]
    public void Pray_RestoresPrayerAndStartsTheFactionTimer(string altar, Type stateType)
    {
        var world = new World();
        world.Clicker.Statistics.HealPrayerPoints(990).Returns(5);

        world.Click(altar, GameObjectClickType.Option1Click);

        Assert.AreEqual(645, world.Animations.Single());
        world.Clicker.Received(1).SendChatMessage("You prayed to the gods and they restored your prayer points.");
        var state = (TimedState)world.AddedStates.Single();
        Assert.AreEqual(stateType, state.GetType());
        Assert.AreEqual(1000, state.TicksLeft);
    }

    [TestMethod]
    [DataRow("armadyl")]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    [DataRow("zamorak")]
    public void Pray_WithFullPrayerPoints_ExplainsAndStartsNoTimer(string altar)
    {
        var world = new World();
        world.Clicker.Statistics.HealPrayerPoints(990).Returns(0);

        world.Click(altar, GameObjectClickType.Option1Click);

        world.Clicker.Received(1).SendChatMessage("You cannot restore any more prayer points.");
        Assert.IsEmpty(world.AddedStates);
        Assert.IsEmpty(world.Animations);
    }

    [TestMethod]
    [DataRow("armadyl")]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    [DataRow("zamorak")]
    public void Pray_WhileTheFactionTimerRuns_Refuses(string altar)
    {
        var world = new World();
        world.StateRunning(altar);

        world.Click(altar, GameObjectClickType.Option1Click);

        world.Clicker.Received(1).SendChatMessage("You cannot pray at this altar yet.");
        world.Clicker.Statistics.DidNotReceiveWithAnyArgs().HealPrayerPoints(default);
        Assert.IsEmpty(world.AddedStates);
    }

    [TestMethod]
    [DataRow("armadyl")]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    [DataRow("zamorak")]
    public void Pray_InCombat_DoesNothing(string altar)
    {
        var world = new World();
        world.Clicker.Combat.IsInCombat().Returns(true);

        world.Click(altar, GameObjectClickType.Option1Click);

        world.Clicker.Statistics.DidNotReceiveWithAnyArgs().HealPrayerPoints(default);
        world.Clicker.DidNotReceiveWithAnyArgs().SendChatMessage(default!);
        Assert.IsEmpty(world.AddedStates);
    }

    [TestMethod]
    [DataRow("armadyl", 2835, 5294)]
    [DataRow("bandos", 2862, 5357)]
    [DataRow("saradomin", 2923, 5257)]
    [DataRow("zamorak", 2925, 5333)]
    public void Option2_TeleportsToTheAltarsDestinationOnGroundPlane(string altar, int x, int y)
    {
        var world = new World();

        world.Click(altar, GameObjectClickType.Option2Click);
        world.RunTeleportTask();

        var location = world.Teleports.Single().Location;
        Assert.AreEqual(x, location.X);
        Assert.AreEqual(y, location.Y);
        Assert.AreEqual(0, location.Z);
        Assert.AreEqual(0, location.Dimension);
    }

    [TestMethod]
    public void Option2_EachFactionTeleportsToItsOwnDestination()
    {
        var destinations = new[] { "armadyl", "bandos", "saradomin", "zamorak" }.Select(altar =>
        {
            var world = new World();
            world.Click(altar, GameObjectClickType.Option2Click);
            world.RunTeleportTask();
            var location = world.Teleports.Single().Location;
            return (location.X, location.Y);
        }).ToArray();

        Assert.HasCount(4, destinations.Distinct());
    }

    [TestMethod]
    public void ObjectIds_AreRegisteredPerAltar()
    {
        CollectionAssert.AreEqual(new[] { 26288 }, Ids<Armadyl.ArmadylAltar>());
        CollectionAssert.AreEqual(new[] { 26289 }, Ids<Bandos.BandosAltar>());
        CollectionAssert.AreEqual(new[] { 26287 }, Ids<Saradomin.SaradominAltar>());
        CollectionAssert.AreEqual(new[] { 26286 }, Ids<Zamorak.ZamorakAltar>());
    }

    private static int[] Ids<T>() => typeof(T).GetCustomAttributes(typeof(GameObjectScriptMetaDataAttribute), false)
        .Cast<GameObjectScriptMetaDataAttribute>().SelectMany(a => a.ObjectIds).ToArray();

    private sealed class World
    {
        public World()
        {
            Clicker = Substitute.For<ICharacter>();
            Clicker.Statistics.Returns(Substitute.For<ICharacterStatistics>());
            Clicker.When(c => c.QueueAnimation(Arg.Any<IAnimation>())).Do(c => Animations.Add(c.Arg<IAnimation>().Id));
            Clicker.When(c => c.AddState(Arg.Any<IState>())).Do(c => AddedStates.Add(c.Arg<IState>()));
            Clicker.When(c => c.QueueTask(Arg.Any<ITaskItem>())).Do(c => Tasks.Add((RsTask)c.Arg<ITaskItem>()));
            Clicker.Movement.When(m => m.Teleport(Arg.Any<ITeleport>())).Do(c => Teleports.Add(c.Arg<ITeleport>()));

            // allow the standard teleport to start
            Clicker.Area.Script.CanDoStandardTeleport(Clicker).Returns(true);
            Clicker.EventManager.SendEvent(Arg.Any<TeleportAllowEvent>()).Returns(true);
            var provider = Substitute.For<IServiceProvider>();
            provider.GetService(typeof(IMapRegionService)).Returns(Substitute.For<IMapRegionService>());
            Clicker.ServiceProvider.Returns(provider);
        }

        public ICharacter Clicker { get; }
        public List<int> Animations { get; } = [];
        public List<IState> AddedStates { get; } = [];
        public List<RsTask> Tasks { get; } = [];
        public List<ITeleport> Teleports { get; } = [];

        public void StateRunning(string altar)
        {
            switch (altar)
            {
                case "armadyl": Clicker.HasState<ArmadylAltarPrayedState>().Returns(true); break;
                case "bandos": Clicker.HasState<BandosAltarPrayedState>().Returns(true); break;
                case "saradomin": Clicker.HasState<SaradominAltarPrayedState>().Returns(true); break;
                default: Clicker.HasState<ZamorakAltarPrayedState>().Returns(true); break;
            }
        }

        public void Click(string altar, GameObjectClickType click)
        {
            GameObjectScript script = altar switch
            {
                "armadyl" => new Armadyl.ArmadylAltar(),
                "bandos" => new Bandos.BandosAltar(),
                "saradomin" => new Saradomin.SaradominAltar(),
                _ => new Zamorak.ZamorakAltar()
            };
            script.Initialize(Substitute.For<IGameObject>());
            script.OnCharacterClickPerform(Clicker, click);
        }

        /// <summary>Runs queued tasks until the teleport happens (the standard teleport delays it by 4 ticks).</summary>
        public void RunTeleportTask()
        {
            for (var tick = 0; tick < 10 && Teleports.Count == 0; tick++)
            {
                foreach (var task in Tasks.ToArray())
                {
                    task.Tick();
                }
            }
        }
    }
}
