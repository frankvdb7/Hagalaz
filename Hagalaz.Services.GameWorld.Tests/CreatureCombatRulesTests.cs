using Hagalaz.Game.Abstractions.Model.Creatures;
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
