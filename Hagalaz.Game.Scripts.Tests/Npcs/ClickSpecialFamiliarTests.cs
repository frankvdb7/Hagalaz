using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using NSubstitute;
using static Hagalaz.Game.Scripts.Tests.SkillTaskTestSupport;

namespace Hagalaz.Game.Scripts.Tests.Npcs;

/// <summary>
///     Pins the Spirit terror bird, Thorny snail and War tortoise familiars: their special move data, and that using
///     the special move consumes the scroll and points, grants experience and fires only when affordable.
///     Expectations are the literals of the original per-familiar implementations.
/// </summary>
[TestClass]
public sealed class ClickSpecialFamiliarTests
{
    private const int ScrollId = 12345;
    private const double ScrollExperience = 11.5;

    // name, npc ids, inventory capacity, special points, type, move name
    [TestMethod]
    [DataRow("terrorBird", 12, 8, FamiliarSpecialType.Click, "Tireless Run")]
    [DataRow("thornySnail", 3, 3, FamiliarSpecialType.Creature, "Slime Spray")]
    [DataRow("warTortoise", 18, 20, FamiliarSpecialType.Click, "Testudo")]
    public void Familiar_DeclaresItsSpecialMoveData(string familiar, int capacity, int points, FamiliarSpecialType type, string moveName)
    {
        var script = new World().Create(familiar);

        Assert.AreEqual(capacity, script.InventoryCapacity);
        Assert.AreEqual(points, script.GetRequiredSpecialMovePoints());
        Assert.AreEqual(type, script.GetSpecialType());
        Assert.AreEqual(moveName, script.GetSpecialMoveName());
        Assert.IsFalse(script.CanPoison());
    }

    [TestMethod]
    [DataRow("warTortoise", "Boosts your Defence level by 8.")]
    [DataRow("terrorBird", "Boosts your Agility level by 2 and restores run energy by half your Agility level.")]
    [DataRow("thornySnail", "Inflicts up to 80 damage against your opponent.")]
    public void Familiar_DescribesItsMove(string familiar, string description)
    {
        Assert.AreEqual(description, new World().Create(familiar).GetSpecialMoveDescription());
    }

    [TestMethod]
    public void Testudo_BoostsDefenceByEight()
    {
        var world = new World();
        var script = world.CreateAttached("warTortoise");
        world.Summoner.Statistics.LevelForExperience(StatisticsConstants.Defence).Returns(70);
        script.SetSpecialMovePoints(20);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        world.Summoner.Statistics.Received(1).HealSkill(StatisticsConstants.Defence, 78, 8);
    }

    [TestMethod]
    [DataRow(51, 53, 26)]
    [DataRow(52, 54, 26)]
    public void TirelessRun_BoostsAgilityAndRestoresHalfTheLevelRoundedUp(int level, int max, int energy)
    {
        var world = new World();
        var script = world.CreateAttached("terrorBird");
        world.Summoner.Statistics.LevelForExperience(StatisticsConstants.Agility).Returns(level);
        script.SetSpecialMovePoints(8);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        world.Summoner.Statistics.Received(1).HealSkill(StatisticsConstants.Agility, max, 2);
        world.Summoner.Statistics.Received(1).HealRunEnergy(energy);
    }

    [TestMethod]
    [DataRow(450, 450)]
    [DataRow(0, 0)]
    [DataRow(1000, 800)]
    public void SlimeSpray_HitsTheTargetForUpToEightyLifePoints(int rolled, int expected)
    {
        var world = new World();
        var script = world.CreateAttached("thornySnail");
        ((INpcCombat)world.NpcCombat).GetMagicDamage(world.Target, 800).Returns(rolled);
        script.SetSpecialMovePoints(3);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        world.NpcCombat.Received(1).PerformAttack(Arg.Is<AttackParams>(p =>
            p!.Target == world.Target.Handle && p.Damage == expected && p.MaxDamage == 800 && p.HitType == HitSplatType.HitMagicDamage));
    }

    [TestMethod]
    public void SlimeSpray_WithoutEnoughPoints_DealsNoDamage()
    {
        var world = new World();
        var script = world.CreateAttached("thornySnail");
        script.SetSpecialMovePoints(2);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        world.NpcCombat.DidNotReceiveWithAnyArgs().PerformAttack(default!);
    }

    [TestMethod]
    public void SlimeSpray_WithoutACreature_DealsNoDamage()
    {
        var world = new World();
        var script = world.CreateAttached("thornySnail");
        script.SetSpecialMovePoints(3);

        script.SetSpecialMoveTarget(null);
        script.SetSpecialMoveTarget(Substitute.For<IRuneObject>());

        world.NpcCombat.DidNotReceiveWithAnyArgs().PerformAttack(default!);
    }

    [TestMethod]
    [DataRow("warTortoise")]
    [DataRow("terrorBird")]
    public void SpecialMoveEffect_WithoutEnoughPoints_IsNotApplied(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        script.SetSpecialMovePoints(script.GetRequiredSpecialMovePoints() - 1);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        world.Summoner.Statistics.DidNotReceiveWithAnyArgs().HealSkill(default, default, default);
        world.Summoner.Statistics.DidNotReceiveWithAnyArgs().HealRunEnergy(default);
    }

    [TestMethod]
    [DataRow("warTortoise")]
    [DataRow("terrorBird")]
    public void SetSpecialMoveTarget_WithoutACreature_AppliesNoEffect(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        script.SetSpecialMovePoints(script.GetRequiredSpecialMovePoints());

        script.SetSpecialMoveTarget(null);

        world.Summoner.Statistics.DidNotReceiveWithAnyArgs().HealSkill(default, default, default);
    }

    [TestMethod]
    public void NpcIds_AreRegisteredPerFamiliar()
    {
        CollectionAssert.AreEqual(new[] { 6794, 6795 }, NpcIds<SpiritTerrorBird>());
        CollectionAssert.AreEqual(new[] { 6806, 6807 }, NpcIds<ThornySnail>());
        CollectionAssert.AreEqual(new[] { 6815, 6816 }, NpcIds<WarTortoise>());
    }

    [TestMethod]
    [DataRow("terrorBird")]
    [DataRow("thornySnail")]
    [DataRow("warTortoise")]
    public void PerformSpecialMove_WithoutEnoughPoints_AnnouncesAndKeepsTheScroll(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        script.SetSpecialMovePoints(script.GetRequiredSpecialMovePoints() - 1);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        world.Summoner.Received(1).SendChatMessage(GameStrings.NotEnoughSpecialMovePoints);
        Assert.IsFalse(script.UsingSpecialMove);
        Assert.AreEqual(1, world.Inventory.Items.GetCountById(ScrollId));
        world.Summoner.Statistics.DidNotReceiveWithAnyArgs().AddExperience(default, default);
    }

    [TestMethod]
    [DataRow("terrorBird")]
    [DataRow("thornySnail")]
    [DataRow("warTortoise")]
    public void PerformSpecialMove_WithPointsAndScroll_ConsumesScrollDrainsPointsAndGrantsExperience(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        var required = script.GetRequiredSpecialMovePoints();
        script.SetSpecialMovePoints(required + 5);
        script.SetUsingSpecialMove(true);

        script.PerformSpecialMove(world.Target);

        Assert.AreEqual(0, world.Inventory.Items.GetCountById(ScrollId));
        Assert.AreEqual(5, script.SpecialMovePoints);
        Assert.IsFalse(script.UsingSpecialMove);
        world.Summoner.Statistics.Received(1).AddExperience(StatisticsConstants.Summoning, ScrollExperience);
    }

    [TestMethod]
    [DataRow("terrorBird")]
    [DataRow("thornySnail")]
    [DataRow("warTortoise")]
    public void PerformAttack_WhileUsingSpecialMove_PerformsTheSpecialMove(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        script.SetSpecialMovePoints(script.GetRequiredSpecialMovePoints());
        script.SetUsingSpecialMove(true);

        script.PerformAttack(world.Target);

        Assert.AreEqual(0, world.Inventory.Items.GetCountById(ScrollId));
        Assert.AreEqual(0, script.SpecialMovePoints);
    }

    [TestMethod]
    [DataRow("terrorBird")]
    [DataRow("thornySnail")]
    [DataRow("warTortoise")]
    public void SetSpecialMoveTarget_WithACreature_ActivatesAndPerformsTheMove(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        script.SetSpecialMovePoints(script.GetRequiredSpecialMovePoints());

        script.SetSpecialMoveTarget(world.Target);

        Assert.AreEqual(0, world.Inventory.Items.GetCountById(ScrollId));
        Assert.AreEqual(0, script.SpecialMovePoints);
        Assert.IsFalse(script.UsingSpecialMove);
    }

    [TestMethod]
    [DataRow("terrorBird")]
    [DataRow("thornySnail")]
    [DataRow("warTortoise")]
    public void SetSpecialMoveTarget_WithoutACreature_DoesNothing(string familiar)
    {
        var world = new World();
        var script = world.CreateAttached(familiar);
        script.SetSpecialMovePoints(script.GetRequiredSpecialMovePoints());

        script.SetSpecialMoveTarget(null);
        script.SetSpecialMoveTarget(Substitute.For<IRuneObject>());

        Assert.AreEqual(1, world.Inventory.Items.GetCountById(ScrollId));
        Assert.AreEqual(script.GetRequiredSpecialMovePoints(), script.SpecialMovePoints);
        Assert.IsFalse(script.UsingSpecialMove);
    }

    private static int[] NpcIds<T>() => typeof(T).GetCustomAttribute<NpcScriptMetaDataAttribute>()!.NpcIds.ToArray();

    private sealed class World
    {
        public World()
        {
            Inventory = new ComposedTestContainer(28);
            Inventory.Items.Add(CreateItem(ScrollId, 1));
            Summoner = Substitute.For<ICharacter>();
            Summoner.Inventory.Returns(Inventory);
            Summoner.Statistics.Returns(Substitute.For<ICharacterStatistics>());
            Target = Substitute.For<ICreature>();
        }

        public ComposedTestContainer Inventory { get; }
        public ICharacter Summoner { get; }
        public ICreature Target { get; }
        public ICreatureCombat NpcCombat { get; } = Substitute.For<ICreatureCombat, INpcCombat>();

        public BobFamiliarScriptBase Create(string familiar)
        {
            var owner = Substitute.For<INpc>();
            owner.Combat.Returns(NpcCombat);
            var factory = Substitute.For<IItemContainerFactory>();
            var pathFinder = Substitute.For<ISmartPathFinder>();
            var npcService = Substitute.For<INpcService>();
            var itemService = Substitute.For<IItemService>();
            var groundItems = Substitute.For<IGroundItemBuilder>();
            IItemBuilder items = new TestItemBuilder(CreateItem);
            var activator = Substitute.For<IWidgetScriptActivator>();
            return familiar switch
            {
                "terrorBird" => new SpiritTerrorBird(owner, factory, pathFinder, npcService, itemService, groundItems, items, activator),
                "thornySnail" => new ThornySnail(owner, factory, pathFinder, npcService, itemService, groundItems, items, activator),
                _ => new WarTortoise(owner, factory, pathFinder, npcService, itemService, groundItems, items, activator)
            };
        }

        public BobFamiliarScriptBase CreateAttached(string familiar)
        {
            var script = Create(familiar);
            script.AttachToSummoner(Summoner, new SummoningDto { ScrollId = ScrollId, ScrollExperience = ScrollExperience });
            return script;
        }
    }
}
