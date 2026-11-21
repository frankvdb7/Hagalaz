using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    /// <summary>
    ///     Pins the item "extra data" getters of <see cref="ItemTypeLogic"/>: value present, absent, wrong type, no extra data.
    /// </summary>
    public class ItemTypeLogicExtraDataTests
    {
        public sealed record Getter(string Name, Func<IItemType, int> Read, int Key, int Default, bool Tenths)
        {
            public override string ToString() => Name;
        }

        public static IEnumerable<object[]> IntGetters()
        {
            yield return Case("GetQuestId", item => item.GetQuestId(), ItemConstants.QuestId, -1);
            yield return Case("GetAttackSpeed", item => item.GetAttackSpeed(), ItemConstants.AttackSpeed, 4);
            yield return Case("GetStabAttack", item => item.GetStabAttack(), ItemConstants.StabAttack, 0);
            yield return Case("GetSlashAttack", item => item.GetSlashAttack(), ItemConstants.SlashAttack, 0);
            yield return Case("GetCrushAttack", item => item.GetCrushAttack(), ItemConstants.CrushAttack, 0);
            yield return Case("GetMagicAttack", item => item.GetMagicAttack(), ItemConstants.MagicAttack, 0);
            yield return Case("GetRangeAttack", item => item.GetRangeAttack(), ItemConstants.RangeAttack, 0);
            yield return Case("GetStabDefence", item => item.GetStabDefence(), ItemConstants.StabDefence, 0);
            yield return Case("GetSlashDefence", item => item.GetSlashDefence(), ItemConstants.SlashDefence, 0);
            yield return Case("GetCrushDefence", item => item.GetCrushDefence(), ItemConstants.CrushDefence, 0);
            yield return Case("GetMagicDefence", item => item.GetMagicDefence(), ItemConstants.MagicDefence, 0);
            yield return Case("GetRangeDefence", item => item.GetRangeDefence(), ItemConstants.RangeDefence, 0);
            yield return Case("GetSummoningDefence", item => item.GetSummoningDefence(), ItemConstants.SummoningDefence, 0);
            yield return Case("GetAbsorbMeleeBonus", item => item.GetAbsorbMeleeBonus(), ItemConstants.AbsorbMeleeBonus, 0);
            yield return Case("GetAbsorbMageBonus", item => item.GetAbsorbMageBonus(), ItemConstants.AbsorbMageBonus, 0);
            yield return Case("GetAbsorbRangeBonus", item => item.GetAbsorbRangeBonus(), ItemConstants.AbsorbRangeBonus, 0);
            yield return Case("GetMagicDamage", item => item.GetMagicDamage(), ItemConstants.MagicDamage, 0);
            yield return Case("GetPrayerBonus", item => item.GetPrayerBonus(), ItemConstants.PrayerBonus, 0);
            yield return Case("GetWeaponType", item => item.GetWeaponType(), ItemConstants.WeaponType, 0);
            yield return Case("GetRenderAnimationId", item => item.GetRenderAnimationId(), ItemConstants.RenderAnimationId, -1);
            yield return Case("GetStrengthBonus", item => item.GetStrengthBonus(), ItemConstants.StrengthBonus, 0, tenths: true);
            yield return Case("GetRangedStrengthBonus", item => item.GetRangedStrengthBonus(), ItemConstants.RangedStrengthBonus, 0, tenths: true);
        }

        [Theory]
        [MemberData(nameof(IntGetters))]
        public void Getter_ReturnsStoredIntValue(Getter getter)
        {
            var item = ItemWith(new Dictionary<int, object> { [getter.Key] = 47 });

            Assert.Equal(getter.Tenths ? 4 : 47, getter.Read(item));
        }

        [Theory]
        [MemberData(nameof(IntGetters))]
        public void Getter_ReturnsDefaultWhenKeyAbsent(Getter getter)
        {
            var item = ItemWith(new Dictionary<int, object> { [getter.Key + 1000] = 47 });

            Assert.Equal(getter.Default, getter.Read(item));
        }

        [Theory]
        [MemberData(nameof(IntGetters))]
        public void Getter_ReturnsDefaultWhenNoExtraData(Getter getter)
        {
            Assert.Equal(getter.Default, getter.Read(ItemWith(null)));
        }

        [Theory]
        [MemberData(nameof(IntGetters))]
        public void Getter_ReturnsDefaultWhenValueIsNotAnInt(Getter getter)
        {
            var item = ItemWith(new Dictionary<int, object> { [getter.Key] = "47" });

            Assert.Equal(getter.Default, getter.Read(item));
        }

        [Theory]
        [MemberData(nameof(IntGetters))]
        public void Getter_ReturnsNegativeStoredValues(Getter getter)
        {
            var item = ItemWith(new Dictionary<int, object> { [getter.Key] = -30 });

            Assert.Equal(getter.Tenths ? -3 : -30, getter.Read(item));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(2, false)]
        public void HasSpecialBar_IsTrueOnlyForStoredOne(int stored, bool expected)
        {
            Assert.Equal(expected, ItemWith(new Dictionary<int, object> { [ItemConstants.HasSpecialBar] = stored }).HasSpecialBar());
        }

        [Fact]
        public void HasSpecialBar_IsFalseWhenAbsentNotAnIntOrNoExtraData()
        {
            Assert.False(ItemWith(new Dictionary<int, object>()).HasSpecialBar());
            Assert.False(ItemWith(new Dictionary<int, object> { [ItemConstants.HasSpecialBar] = "1" }).HasSpecialBar());
            Assert.False(ItemWith(null).HasSpecialBar());
        }

        private static ItemType ItemWith(IReadOnlyDictionary<int, object>? extraData) => new(1) { ExtraData = extraData };

        private static object[] Case(string name, Func<IItemType, int> read, int key, int defaultValue, bool tenths = false) =>
            [new Getter(name, read, key, defaultValue, tenths)];
    }
}
