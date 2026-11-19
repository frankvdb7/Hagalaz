using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Minigames.Barrows.NPCs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Barrows;

[TestClass]
public sealed class AkrisaeCombatTests
{
    [TestMethod]
    public void Protection_StartsWithNoneAndLeavesDamageUntouched()
    {
        var protection = new AkrisaeProtection();

        Assert.AreEqual(AkrisaeStyle.None, protection.Protected);
        Assert.AreEqual(100, protection.Reduce(DamageType.StandardMelee, 100));
    }

    [TestMethod]
    public void Protection_SwitchesToTheStyleThatReachedTheThreshold()
    {
        var protection = new AkrisaeProtection();

        Assert.IsFalse(protection.Record(DamageType.StandardMagic, AkrisaeProtection.SwitchDamage - 1));
        Assert.IsTrue(protection.Record(DamageType.FullMagic, 1));

        Assert.AreEqual(AkrisaeStyle.Magic, protection.Protected);
        Assert.AreEqual(60, protection.Reduce(DamageType.StandardMagic, 100));
        Assert.AreEqual(100, protection.Reduce(DamageType.StandardRange, 100));
        Assert.AreEqual(100, protection.Reduce(DamageType.StandardMelee, 100));
    }

    [TestMethod]
    public void Protection_CountsEachStyleSeparatelyAndIgnoresMisses()
    {
        var protection = new AkrisaeProtection();

        Assert.IsFalse(protection.Record(DamageType.StandardMelee, 100));
        Assert.IsFalse(protection.Record(DamageType.StandardRange, 100));
        Assert.IsFalse(protection.Record(DamageType.StandardMagic, -1));
        Assert.IsFalse(protection.Record(DamageType.StandardMagic, 0));
        Assert.AreEqual(AkrisaeStyle.None, protection.Protected);
    }

    [TestMethod]
    public void Protection_HittingTheAlreadyProtectedStyleDoesNotSwitchAgain()
    {
        var protection = new AkrisaeProtection();
        protection.Record(DamageType.StandardRange, AkrisaeProtection.SwitchDamage);

        Assert.IsFalse(protection.Record(DamageType.StandardRange, AkrisaeProtection.SwitchDamage));
        Assert.AreEqual(AkrisaeStyle.Ranged, protection.Protected);
    }

    [TestMethod]
    public void Protection_ResetClearsProtectionAndCounters()
    {
        var protection = new AkrisaeProtection();
        protection.Record(DamageType.StandardMelee, AkrisaeProtection.SwitchDamage);
        protection.Record(DamageType.StandardRange, 100);

        protection.Reset();

        Assert.AreEqual(AkrisaeStyle.None, protection.Protected);
        Assert.IsFalse(protection.Record(DamageType.StandardRange, 100));
    }

    [TestMethod]
    public void Script_SwitchingProtectionSkipsHisNextAttackAndReducesProtectedHits()
    {
        var owner = Substitute.For<INpc>();
        var script = new AkrisaeTheDoomed(owner, Substitute.For<INpcService>(), Substitute.For<ISimplePathFinder>(),
            Substitute.For<IWidgetScriptActivator>());
        var attacker = Substitute.For<ICreature>();

        script.OnAttack(attacker, DamageType.StandardMagic, AkrisaeProtection.SwitchDamage);

        owner.Combat.Received(1).ResetCombatDelay();
        Assert.AreEqual(60, script.OnIncomingAttack(attacker, DamageType.StandardMagic, 100, 0));
        Assert.AreEqual(100, script.OnIncomingAttack(attacker, DamageType.StandardMelee, 100, 0));
    }

    [TestMethod]
    public void Script_AttacksWithMelee()
    {
        var script = new AkrisaeTheDoomed(Substitute.For<INpc>(), Substitute.For<INpcService>(), Substitute.For<ISimplePathFinder>(),
            Substitute.For<IWidgetScriptActivator>());

        Assert.AreEqual(AttackBonus.Crush, script.GetAttackBonusType());
        Assert.AreEqual(AttackStyle.MeleeAccurate, script.GetAttackStyle());
    }

    [TestMethod]
    public void Ahrim_BlightedAuraDrainsFiveStrengthLevelsOnlyWhenTheRollIsUnderTwentyPercent()
    {
        var hit = Substitute.For<ICharacter>();
        var miss = Substitute.For<ICharacter>();

        AhrimTheBlighted.ApplyBlightedAura(hit, 0.19);
        AhrimTheBlighted.ApplyBlightedAura(miss, 0.20);

        hit.Statistics.Received(1).DamageSkill(StatisticsConstants.Strength, 5);
        miss.Statistics.DidNotReceive().DamageSkill(Arg.Any<int>(), Arg.Any<int>());
    }
}
