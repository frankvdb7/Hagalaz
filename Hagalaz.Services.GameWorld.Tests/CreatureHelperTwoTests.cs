using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureHelperTwoTests
{
    [TestMethod]
    [DataRow(7, 8, 1.0, 7, DisplayName = "single possible damage roll")]
    [DataRow(7, 7, 1.0, 7, DisplayName = "equal bounds")]
    [DataRow(8, 7, 1.0, 8, DisplayName = "reversed bounds")]
    public void DamageCalculations_ReturnExpectedDamage(int minimum, int maximum, double accuracy, int expected)
    {
        var magicDamage = CreatureHelperTwo.PerformMagicDamageCalculation(minimum, maximum, accuracy);
        var meleeDamage = CreatureHelperTwo.PerformMeleeDamageCalculation(minimum, maximum, accuracy);
        var rangedDamage = CreatureHelperTwo.PerformRangedDamageCalculation(minimum, maximum, accuracy);

        Assert.AreEqual(expected, magicDamage, "Magic damage");
        Assert.AreEqual(expected, meleeDamage, "Melee damage");
        Assert.AreEqual(expected, rangedDamage, "Ranged damage");
    }
}
