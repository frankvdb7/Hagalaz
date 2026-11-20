using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

/// <summary>
///     Pins Armadyl Godwars NPC attack numbers and the "flying too high for melee" rule.
///     Expected values are the literals of the original per-NPC implementations.
/// </summary>
[TestClass]
public sealed class ArmadylNpcTests
{
    private const string FlyingMessage = "The aviansie is flying too high for you to attack using melee.";

    // ---------- Attacks (target at dx=3, dy=1 from the owner) ----------

    [TestMethod]
    public void ArmadylFaction_FiresRangedProjectileAndAttacksWithRangeDamage()
    {
        var world = new NpcAttackWorld();
        var script = new ArmadylFaction(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1190, duration: 40, delay: 30, fromHeight: 50);
        world.AssertRangedAttack(delay: 40, damage: 7, maxDamage: 9);
        Assert.AreEqual(AttackBonus.Ranged, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.RangedAccurate, script.GetAttackStyle());
        Assert.AreEqual(8, script.GetAttackDistance());
    }

    [TestMethod]
    public void FlockleaderGeerin_FiresRangedProjectileAndAttacksWithRangeDamage()
    {
        var world = new NpcAttackWorld();
        var script = new FlockleaderGeerin(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1190, duration: 35, delay: 25, fromHeight: 50);
        world.AssertRangedAttack(delay: 35, damage: 7, maxDamage: 9);
        Assert.AreEqual(AttackBonus.Ranged, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.RangedAccurate, script.GetAttackStyle());
        Assert.AreEqual(7, script.GetAttackDistance());
    }

    [TestMethod]
    public void WingmanSkree_FiresProjectileAndAttacksWithMagicBase250()
    {
        var world = new NpcAttackWorld();
        var script = new WingmanSkree(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1190, duration: 35, delay: 25, fromHeight: 50);
        world.AssertMagicAttack(delay: 35, baseDamage: 250);
        Assert.AreEqual(AttackBonus.Magic, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.MagicNormal, script.GetAttackStyle());
        Assert.AreEqual(7, script.GetAttackDistance());
    }

    [TestMethod]
    public void ArmadylSpiritualMage_AttacksWithMagicBase146AndNoProjectile()
    {
        var world = new NpcAttackWorld();
        var script = new SpiritualMage(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        Assert.IsEmpty(world.ProjectileCalls);
        world.AssertMagicAttack(delay: 40, baseDamage: 146);
        Assert.AreEqual(AttackBonus.Magic, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.MagicNormal, script.GetAttackStyle());
        Assert.AreEqual(8, script.GetAttackDistance());
    }

    // ---------- Melee denial ----------

    [TestMethod]
    [DataRow("faction", AttackStyle.MeleeAccurate)]
    [DataRow("faction", AttackStyle.MeleeAggressive)]
    [DataRow("faction", AttackStyle.MeleeControlled)]
    [DataRow("faction", AttackStyle.MeleeDefensive)]
    [DataRow("geerin", AttackStyle.MeleeAccurate)]
    [DataRow("skree", AttackStyle.MeleeDefensive)]
    [DataRow("kilisa", AttackStyle.MeleeAggressive)]
    [DataRow("kreearra", AttackStyle.MeleeControlled)]
    public void MeleeAttacker_IsDeniedWithMessageAndOwnerRetaliates(string npc, AttackStyle style)
    {
        var world = new NpcAttackWorld();
        var attacker = world.CreateCharacterAttacker(style);

        var allowed = CreateNpc(world, npc).CanBeAttackedBy(attacker);

        Assert.IsFalse(allowed);
        attacker.Received(1).SendChatMessage(FlyingMessage);
        world.Owner.Received(1).QueueTask(Arg.Any<ITaskItem>());
    }

    [TestMethod]
    public void MeleeAttacker_WhoseTargetMayNotBeRetaliatedAgainst_IsDeniedWithoutRetaliation()
    {
        var world = new NpcAttackWorld();
        var attacker = world.CreateCharacterAttacker(AttackStyle.MeleeAccurate);
        var script = new NoRetaliationAviansie(world);

        Assert.IsFalse(script.CanBeAttackedBy(attacker));

        attacker.Received(1).SendChatMessage(FlyingMessage);
        world.Owner.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());
    }

    [TestMethod]
    public void MeleeNpcAttacker_IsDeniedWithoutChatMessage()
    {
        var world = new NpcAttackWorld();
        var attacker = Substitute.For<ICreature>();
        attacker.Combat.GetAttackStyle().Returns(AttackStyle.MeleeAccurate);

        Assert.IsFalse(new ArmadylFaction(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator)
            .CanBeAttackedBy(attacker));

        world.Owner.Received(1).QueueTask(Arg.Any<ITaskItem>());
    }

    [TestMethod]
    [DataRow("faction")]
    [DataRow("geerin")]
    [DataRow("skree")]
    [DataRow("kilisa")]
    [DataRow("kreearra")]
    public void RangedAttacker_FallsThroughToBaseRulesWithoutMessageOrRetaliation(string npc)
    {
        var world = new NpcAttackWorld();
        var attacker = world.CreateCharacterAttacker(AttackStyle.RangedAccurate);

        CreateNpc(world, npc).CanBeAttackedBy(attacker);

        attacker.DidNotReceive().SendChatMessage(FlyingMessage);
        world.Owner.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());
    }

    private static NpcScriptBase CreateNpc(NpcAttackWorld world, string npc) => npc switch
    {
        "faction" => new ArmadylFaction(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator),
        "geerin" => new FlockleaderGeerin(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator),
        "skree" => new WingmanSkree(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator),
        "kilisa" => new FlightKilisa(world.Owner, world.NpcService, world.PathFinder, world.WidgetActivator),
        "kreearra" => new Kreearra(world.Owner, Substitute.For<ILocationBuilder>(), Substitute.For<IAnimationBuilder>(),
            Substitute.For<IMovementBuilder>(), world.Projectiles, Substitute.For<IMapRegionService>(), world.NpcService, world.PathFinder,
            world.WidgetActivator),
        _ => throw new System.ArgumentOutOfRangeException(nameof(npc))
    };

    private sealed class NoRetaliationAviansie(NpcAttackWorld world)
        : ArmadylFaction(world.Owner, world.Projectiles, world.NpcService, world.PathFinder, world.WidgetActivator)
    {
        public override bool CanRetaliateTo(ICreature creature) => false;
    }
}
