using Hagalaz.Game.Abstractions.Model.Combat;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Extensions.Tests;

[TestClass]
public class CombatExtensionsTests
{
    [TestMethod]
    [DataRow(DamageType.StandardMelee, HitSplatType.HitMeleeDamage)]
    [DataRow(DamageType.StandardRange, HitSplatType.HitRangeDamage)]
    [DataRow(DamageType.StandardMagic, HitSplatType.HitMagicDamage)]
    [DataRow(DamageType.StandardSummoning, HitSplatType.HitSimpleDamage)]
    [DataRow(DamageType.Reflected, HitSplatType.HitDeflectDamage)]
    [DataRow(DamageType.Standard, HitSplatType.HitSimpleDamage)]
    [DataRow(DamageType.DragonFire, HitSplatType.HitMagicDamage)]
    [DataRow(DamageType.FullMelee, HitSplatType.HitMeleeDamage)]
    [DataRow(DamageType.FullRange, HitSplatType.HitRangeDamage)]
    [DataRow(DamageType.FullMagic, HitSplatType.HitMagicDamage)]
    [DataRow(DamageType.FullSummoning, HitSplatType.HitSimpleDamage)]
    [DataRow((DamageType)999, HitSplatType.None)]
    public void ToHitSplatType_ReturnsCorrectHitSplatType(DamageType damageType, HitSplatType expectedHitSplatType)
    {
        var hitSplatType = damageType.ToHitSplatType();

        Assert.AreEqual(expectedHitSplatType, hitSplatType);
    }
}