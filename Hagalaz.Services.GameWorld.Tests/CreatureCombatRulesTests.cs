using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureCombatRulesTests
{
    private readonly ICreatureCombatRules _rules = new CreatureCombatRules();

    [TestMethod]
    [DataRow(7, 8, 1.0, 7, DisplayName = "single possible damage roll")]
    [DataRow(7, 7, 1.0, 7, DisplayName = "equal bounds")]
    [DataRow(8, 7, 1.0, 8, DisplayName = "reversed bounds")]
    public void DamageCalculations_WhenOnlyOneRollIsPossible_ReturnExpectedDamage(int minimum, int maximum, double accuracy, int expected)
    {
        var magicDamage = _rules.PerformMagicDamageCalculation(minimum, maximum, accuracy);
        var meleeDamage = _rules.PerformMeleeDamageCalculation(minimum, maximum, accuracy);
        var rangedDamage = _rules.PerformRangedDamageCalculation(minimum, maximum, accuracy);

        Assert.AreEqual(expected, magicDamage, "Magic damage");
        Assert.AreEqual(expected, meleeDamage, "Melee damage");
        Assert.AreEqual(expected, rangedDamage, "Ranged damage");
    }

    [TestMethod]
    public void DamageCalculations_WhenDamageCanVary_StayWithinConfiguredBounds()
    {
        var magicDamage = _rules.PerformMagicDamageCalculation(10, 20, 1.0);
        var meleeDamage = _rules.PerformMeleeDamageCalculation(10, 20, 1.0);
        var rangedDamage = _rules.PerformRangedDamageCalculation(10, 20, 1.0);

        Assert.IsInRange(10, 19, magicDamage, "Magic damage");
        Assert.IsInRange(10, 19, meleeDamage, "Melee damage");
        Assert.IsInRange(10, 19, rangedDamage, "Ranged damage");
    }

    [TestMethod]
    [DataRow(10.0, 10, 0.0, 0.0, 0.5, DisplayName = "equal rolls")]
    [DataRow(10.0, 0, 0.0, 0.0, 0.775, DisplayName = "attack roll is higher")]
    [DataRow(0.0, 20, 0.0, 0.0, 79.0 / 560.0, DisplayName = "defence roll is higher")]
    public void CalculateRangedAccuracy_ReturnsExpectedValues(
        double attackLevel,
        int defenceLevel,
        double attackBonus,
        double defenceBonus,
        double expected)
    {
        var rangedAccuracy = _rules.CalculateRangedAccuracy(attackLevel, defenceLevel, attackBonus, defenceBonus);

        Assert.AreEqual(expected, rangedAccuracy, 0.0000000001, "Ranged accuracy");
    }

    [TestMethod]
    [DataRow(10.0, 10, AttackStyle.None, AttackStyle.None, 1.0, 1.0, 0.5, DisplayName = "equal rolls")]
    [DataRow(10.0, 10, AttackStyle.MeleeAccurate, AttackStyle.None, 1.0, 1.0, 239.0 / 420.0, DisplayName = "accurate attack style")]
    [DataRow(10.0, 10, AttackStyle.MeleeControlled, AttackStyle.None, 1.0, 1.0, 199.0 / 380.0, DisplayName = "controlled attack style")]
    [DataRow(10.0, 10, AttackStyle.None, AttackStyle.MeleeDefensive, 1.0, 1.0, 179.0 / 420.0, DisplayName = "defensive target style")]
    [DataRow(10.0, 10, AttackStyle.None, AttackStyle.MeleeControlled, 1.0, 1.0, 179.0 / 380.0, DisplayName = "controlled target style")]
    [DataRow(10.0, 0, AttackStyle.None, AttackStyle.None, 1.1, 1.0, 299.0 / 380.0, DisplayName = "void melee multiplier")]
    [DataRow(10.0, 0, AttackStyle.None, AttackStyle.None, 1.0, 1.1, 35.0 / 44.0, DisplayName = "special attack roll multiplier")]
    public void CalculateMeleeAccuracy_AppliesStyleAndAttackLevelAdjustments(
        double attackLevel,
        int defenceLevel,
        AttackStyle attackStyle,
        AttackStyle targetDefenceStyle,
        double attackLevelMultiplier,
        double attackRollMultiplier,
        double expected)
    {
        var accuracy = _rules.CalculateMeleeAccuracy(
            attackLevel,
            defenceLevel,
            attackStyle,
            targetDefenceStyle,
            0.0,
            0.0,
            attackLevelMultiplier,
            attackRollMultiplier);

        Assert.AreEqual(expected, accuracy, 0.0000000001);
    }

    [TestMethod]
    [DataRow(10.0, 0, 53, 0.0, 0.0, 0.5, DisplayName = "equal rolls")]
    [DataRow(0.0, 0, 0, 0.0, 0.0, 0.86875, DisplayName = "attack roll is higher")]
    [DataRow(0.0, 100, 100, 0.0, 0.0, 79.0 / 2040.0, DisplayName = "defence roll is higher")]
    public void CalculateMagicAccuracy_ReturnsExpectedAccuracy(
        double attackLevel,
        int magicDefenceLevel,
        int defenceLevel,
        double attackBonus,
        double defenceBonus,
        double expected)
    {
        var accuracy = _rules.CalculateMagicAccuracy(attackLevel, magicDefenceLevel, defenceLevel, attackBonus, defenceBonus);

        Assert.AreEqual(expected, accuracy, 0.0000000001);
    }

    [TestMethod]
    [DataRow(BonusPrayerType.CurseAttack, "Attack")]
    [DataRow(BonusPrayerType.CurseStrength, "Strength")]
    [DataRow(BonusPrayerType.CurseDefence, "Defence")]
    [DataRow(BonusPrayerType.CurseRanged, "Ranged")]
    [DataRow(BonusPrayerType.CurseMagic, "Magic")]
    [DataRow(BonusPrayerType.StaticAttack, "Unknown")]
    public void GetCurseSkillName_ReturnsExpectedName(BonusPrayerType curseType, string expected)
    {
        var name = _rules.GetCurseSkillName(curseType);

        Assert.AreEqual(expected, name);
    }
}
