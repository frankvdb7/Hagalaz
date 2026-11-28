using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;
using Saradomin = Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

/// <summary>
///     Pins the Bandos and Saradomin spiritual mage/ranger attacks and the shared <see cref="NpcAttackService"/>.
///     World geometry: owner (10,10), target (13,11), so the distance part of the delay is 20.
/// </summary>
[TestClass]
public sealed class SpiritualNpcTests
{
    // ---------- NpcAttackService ----------

    [TestMethod]
    public void Ranged_QueuesRangedHitWithDefaultBaseDelay()
    {
        var world = new NpcAttackWorld();

        world.AttackService.PerformRanged(world.Owner, world.Target);

        world.AssertRangedAttack(delay: 40, damage: 7, maxDamage: 9);
    }

    [TestMethod]
    public void Ranged_HonoursBaseDelay()
    {
        var world = new NpcAttackWorld();

        world.AttackService.PerformRanged(world.Owner, world.Target, baseDelay: 15);

        world.AssertRangedAttack(delay: 35, damage: 7, maxDamage: 9);
    }

    [TestMethod]
    public void Magic_DerivesHitAndMaxHitFromBaseDamage()
    {
        var world = new NpcAttackWorld();

        world.AttackService.PerformMagic(world.Owner, world.Target, baseDamage: 164);

        world.AssertMagicAttack(delay: 40, baseDamage: 164);
    }

    [TestMethod]
    [DataRow(13, 11, 20, 40)]
    [DataRow(7, 9, 20, 40)]
    [DataRow(10, 10, 20, 20)]
    [DataRow(13, 11, 15, 35)]
    public void Delay_IsBasePlusFiveTicksPerTileInEitherDirection(int targetX, int targetY, int baseDelay, int expected)
    {
        var world = new NpcAttackWorld();
        world.Target.Location.Returns(Location.Create(targetX, targetY));

        Assert.AreEqual(expected, NpcAttackDelay.FromDistance(world.Owner, world.Target, baseDelay));
    }

    // ---------- Spiritual NPCs ----------

    [TestMethod]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    public void SpiritualMage_AttacksWithMagicBase146(string faction)
    {
        var world = new NpcAttackWorld();
        var script = CreateMage(faction, world);

        script.PerformAttack(world.Target);

        world.AssertMagicAttack(delay: 40, baseDamage: 146);
        Assert.AreEqual(AttackBonus.Magic, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.MagicNormal, script.GetAttackStyle());
        Assert.AreEqual(8, script.GetAttackDistance());
    }

    [TestMethod]
    [DataRow("bandos")]
    [DataRow("saradomin")]
    public void SpiritualRanger_AttacksWithRangeDamage(string faction)
    {
        var world = new NpcAttackWorld();
        var script = CreateRanger(faction, world);

        script.PerformAttack(world.Target);

        world.AssertRangedAttack(delay: 40, damage: 7, maxDamage: 9);
        Assert.AreEqual(AttackBonus.Ranged, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.RangedAccurate, script.GetAttackStyle());
        Assert.AreEqual(8, script.GetAttackDistance());
    }

    private NpcScriptBase CreateMage(string faction, NpcAttackWorld world) => faction == "bandos"
        ? new SpiritualMage(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator)
        : new Saradomin.SpiritualMage(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator);

    private NpcScriptBase CreateRanger(string faction, NpcAttackWorld world) => faction == "bandos"
        ? new SpiritualRanger(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator)
        : new Saradomin.SpiritualRanger(world.Owner, world.AttackService, world.NpcService, world.PathFinder, world.WidgetActivator);
}
