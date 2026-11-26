using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Npcs.Bosses.Dagannoth;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

/// <summary>
///     Pins the shared single-target projectile attack skeleton and the NPC scripts that use it.
///     World geometry: owner (10,10), target (13,11), so the distance part of the delay is 20.
/// </summary>
[TestClass]
public sealed class NpcProjectileAttackTests
{
    // ---------- NpcAttackDelay ----------

    [TestMethod]
    [DataRow(13, 11, 20, 40)]
    [DataRow(7, 9, 20, 40)]
    [DataRow(10, 10, 20, 20)]
    [DataRow(13, 11, 15, 35)]
    [DataRow(13, 11, 0, 20)]
    public void Delay_IsBasePlusFiveTicksPerTileOfDistanceInEitherDirection(int targetX, int targetY, int baseDelay, int expected)
    {
        var world = new NpcAttackWorld();
        world.Target.Location.Returns(Location.Create(targetX, targetY));

        Assert.AreEqual(expected, NpcAttackDelay.FromDistance(world.Owner, world.Target, baseDelay));
    }

    [TestMethod]
    public void Delay_DefaultsToTwentyBase()
    {
        var world = new NpcAttackWorld();

        Assert.AreEqual(40, NpcAttackDelay.FromDistance(world.Owner, world.Target));
    }

    // ---------- Shared skeleton ----------

    [TestMethod]
    public void Ranged_FiresProjectileThenQueuesRangedHitWithSameDelay()
    {
        var world = new NpcAttackWorld();

        world.AttackService.PerformRanged(world.Owner, world.Target, new NpcProjectile(1206, 50));

        world.AssertProjectile(graphic: 1206, duration: 40, delay: 50);
        world.AssertRangedAttack(delay: 40, damage: 7, maxDamage: 9);
    }

    [TestMethod]
    public void Ranged_HonoursBaseDelayAndProjectileGeometry()
    {
        var world = new NpcAttackWorld();

        world.AttackService.PerformRanged(world.Owner, world.Target,
            new NpcProjectile(1190, 25, FromHeight: 50, ToHeight: 30, Angle: 90), baseDelay: 15);

        world.AssertProjectile(graphic: 1190, duration: 35, delay: 25, fromHeight: 50, toHeight: 30, angle: 90);
        world.AssertRangedAttack(delay: 35, damage: 7, maxDamage: 9);
    }

    [TestMethod]
    public void Magic_UsesGivenBaseDamageForHitAndMaxHit()
    {
        var world = new NpcAttackWorld();

        world.AttackService.PerformMagic(world.Owner, world.Target, new NpcProjectile(1203, 50), baseDamage: 164);

        world.AssertProjectile(graphic: 1203, duration: 40, delay: 50);
        world.AssertMagicAttack(delay: 40, baseDamage: 164);
    }

    // ---------- Scripts using the skeleton ----------

    [TestMethod]
    public void SergeantGrimSpike_FiresRangedProjectile1206()
    {
        var world = new NpcAttackWorld();
        var script = new SergeantGrimSpike(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1206, duration: 40, delay: 50);
        world.AssertRangedAttack(delay: 40, damage: 7, maxDamage: 9);
    }

    [TestMethod]
    public void SergeantSteelWill_FiresMagicProjectile1203WithBase164()
    {
        var world = new NpcAttackWorld();
        var script = new SergeantSteelWill(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1203, duration: 40, delay: 50);
        world.AssertMagicAttack(delay: 40, baseDamage: 164);
    }

    [TestMethod]
    public void DagannothPrime_FiresMagicProjectile1203WithBase610()
    {
        var world = new NpcAttackWorld();
        var script = new DagannothPrime(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1203, duration: 40, delay: 50);
        world.AssertMagicAttack(delay: 40, baseDamage: 610);
    }

    [TestMethod]
    public void DagannothSupreme_FiresRangedProjectile1206()
    {
        var world = new NpcAttackWorld();
        var script = new DagannothSupreme(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator);

        script.PerformAttack(world.Target);

        world.AssertProjectile(graphic: 1206, duration: 40, delay: 50);
        world.AssertRangedAttack(delay: 40, damage: 7, maxDamage: 9);
    }
}
