using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;
using Armadyl = Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl;
using Bandos = Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos;
using Saradomin = Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin;
using Zamorak = Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zamorak;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

/// <summary>
///     Pins the Godwars faction rules: a faction member never aggros its own faction or familiars, only
///     targets creatures within attack range plus one that fewer than three creatures recently attacked, and
///     (Bandos, Saradomin, Zamorak) picks the first such creature when idle.
/// </summary>
[TestClass]
public sealed class GodwarsFactionTests
{
    [TestMethod]
    [DataRow("bandos", 2)]
    [DataRow("saradomin", 2)]
    [DataRow("zamorak", 2)]
    [DataRow("armadyl", 9)]
    public void IsAggressiveTowards_RequiresTheCreatureWithinAttackRangePlusOne(string faction, int range)
    {
        var world = new World();
        var script = world.Create(faction);
        var creature = world.Character(attackers: 0);
        world.Owner.WithinRange(creature, Arg.Any<int>()).Returns(false);
        world.Owner.WithinRange(creature, range).Returns(true);

        Assert.IsTrue(script.IsAggressiveTowards(creature));

        world.Owner.WithinRange(creature, range).Returns(false);
        Assert.IsFalse(script.IsAggressiveTowards(creature));
    }

    [TestMethod]
    [DataRow("bandos", 0, true)]
    [DataRow("bandos", 2, true)]
    [DataRow("bandos", 3, false)]
    [DataRow("saradomin", 2, true)]
    [DataRow("saradomin", 3, false)]
    [DataRow("zamorak", 2, true)]
    [DataRow("zamorak", 3, false)]
    [DataRow("armadyl", 2, true)]
    [DataRow("armadyl", 3, false)]
    public void IsAggressiveTowards_Characters_OnlyWhenAtMostTwoRecentAttackers(string faction, int attackers, bool expected)
    {
        var world = new World();

        Assert.AreEqual(expected, world.Create(faction).IsAggressiveTowards(world.Character(attackers)));
    }

    [TestMethod]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    [DataRow("zamorak")]
    [DataRow("armadyl")]
    public void IsAggressiveTowards_NeverTargetsItsOwnFactionOrFamiliars(string faction)
    {
        var world = new World();
        var script = world.Create(faction);

        Assert.IsFalse(script.IsAggressiveTowards(world.Npc(world.Create(faction), attackers: 0)));
        Assert.IsFalse(script.IsAggressiveTowards(world.Npc(world.Familiar(), attackers: 0)));
    }

    [TestMethod]
    [DataRow("bandos", "saradomin")]
    [DataRow("saradomin", "zamorak")]
    [DataRow("zamorak", "armadyl")]
    [DataRow("armadyl", "bandos")]
    public void IsAggressiveTowards_OtherFactionNpcs_AreTargetsWhenLightlyAttacked(string faction, string other)
    {
        var world = new World();
        var script = world.Create(faction);

        Assert.IsTrue(script.IsAggressiveTowards(world.Npc(world.Create(other), attackers: 2)));
        Assert.IsFalse(script.IsAggressiveTowards(world.Npc(world.Create(other), attackers: 3)));
    }

    [TestMethod]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    [DataRow("zamorak")]
    public void AggressivenessTick_PicksTheFirstAggressiveTargetableCreature(string faction)
    {
        var world = new World();
        var script = world.Create(faction);
        var blocked = world.Character(attackers: 0, handle: 1);
        var tooBusy = world.Character(attackers: 5, handle: 2);
        var chosen = world.Character(attackers: 0, handle: 3);
        var later = world.Character(attackers: 0, handle: 4);
        world.Owner.Combat.CanSetTarget(blocked.Handle).Returns(false);
        world.See(blocked, tooBusy, chosen, later);

        script.AggressivenessTick();

        world.Owner.Combat.Received(1).SetTarget(chosen.Handle);
        world.Owner.Combat.DidNotReceive().SetTarget(later.Handle);
    }

    [TestMethod]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    [DataRow("zamorak")]
    public void AggressivenessTick_DoesNothingInCombatOrWithoutCharactersAround(string faction)
    {
        var inCombat = new World();
        inCombat.Owner.Combat.IsInCombat().Returns(true);
        inCombat.See(inCombat.Character(attackers: 0, handle: 1));
        inCombat.Create(faction).AggressivenessTick();
        inCombat.Owner.Combat.DidNotReceiveWithAnyArgs().SetTarget(default);

        var alone = new World();
        alone.See(alone.Npc(alone.Create("armadyl"), attackers: 0, handle: 1));
        alone.Create(faction).AggressivenessTick();
        alone.Owner.Combat.DidNotReceiveWithAnyArgs().SetTarget(default);
    }

    [TestMethod]
    public void NpcIds_AreRegisteredPerFaction()
    {
        CollectionAssert.AreEqual(new[] { 6254, 6255, 6258, 6259 }, Ids<Saradomin.SaradominFaction>());
        CollectionAssert.AreEqual(new[] { 6210, 6211, 6212, 6213, 6214, 6215, 6216, 6217, 6218, 6219, 6220, 6221 }, Ids<Zamorak.ZamorakFaction>());
        CollectionAssert.AreEqual(new[] { 6268, 6269, 6270, 6271, 6272, 6273, 6274, 6275, 6277, 6279, 6280, 6281, 6282, 6283, 9185, 374 }, Ids<Bandos.BandosFaction>());
        CollectionAssert.AreEqual(new[] { 6229, 6230, 6232, 6233, 6234, 6235, 6236, 6237, 6238, 6239, 6240, 6241, 6242, 6243, 6244, 6245, 6246 }, Ids<Armadyl.ArmadylFaction>());
    }

    private static int[] Ids<T>() => typeof(T).GetCustomAttribute<NpcScriptMetaDataAttribute>()!.NpcIds.ToArray();

    private sealed class World
    {
        private int _nextHandle = 100;

        public World()
        {
            Owner = Substitute.For<INpc>();
            Owner.Combat.Returns(Substitute.For<ICreatureCombat>());
            Owner.Combat.CanSetTarget(Arg.Any<EntityHandle<ICreature>>()).Returns(true);
            Owner.WithinRange(Arg.Any<ICreature>(), Arg.Any<int>()).Returns(true);
            Owner.Viewport.Returns(Substitute.For<IViewport>());
            Owner.Viewport.VisibleCreatures.Returns(new List<ICreature>());
        }

        public INpc Owner { get; }

        public NpcScriptBase Create(string faction)
        {
            var npcService = Substitute.For<INpcService>();
            var pathFinder = Substitute.For<ISimplePathFinder>();
            var activator = Substitute.For<IWidgetScriptActivator>();
            return faction switch
            {
                "bandos" => new Bandos.BandosFaction(Owner, npcService, pathFinder, activator),
                "saradomin" => new Saradomin.SaradominFaction(Owner, npcService, pathFinder, activator),
                "zamorak" => new Zamorak.ZamorakFaction(Owner, npcService, pathFinder, activator),
                _ => new Armadyl.ArmadylFaction(Owner, Substitute.For<INpcAttackService>(), Substitute.For<IAviansieFlightRule>(), npcService, pathFinder, activator)
            };
        }

        public NpcScriptBase Familiar() => new TestFamiliar(
            Substitute.For<INpc>(), Substitute.For<ISmartPathFinder>(), Substitute.For<INpcService>(), Substitute.For<IItemService>(),
            Substitute.For<IItemBuilder>(), Substitute.For<IWidgetScriptActivator>());

        public ICreature Character(int attackers, int handle = -1) => Make(Substitute.For<ICharacter>(), attackers, handle);

        public ICreature Npc(NpcScriptBase script, int attackers, int handle = -1)
        {
            var npc = Substitute.For<INpc>();
            npc.Script.Returns(script);
            return Make(npc, attackers, handle);
        }

        public void See(params ICreature[] creatures) => Owner.Viewport.VisibleCreatures.Returns(creatures.ToList());

        private ICreature Make(ICreature creature, int attackers, int handle)
        {
            creature.Combat.Returns(Substitute.For<ICreatureCombat>());
            creature.Combat.RecentAttackers.Returns(Enumerable.Range(0, attackers).Select(_ => Substitute.For<ICreatureAttackerInfo>()).ToList());
            creature.Handle.Returns(new EntityHandle<ICreature>(handle < 0 ? _nextHandle++ : handle, 1));
            return creature;
        }
    }

    private sealed class TestFamiliar(
        INpc owner, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService, IItemBuilder itemBuilder,
        IWidgetScriptActivator widgetScriptActivator)
        : FamiliarScriptBase(owner, pathFinder, npcService, itemService, itemBuilder, widgetScriptActivator);
}
