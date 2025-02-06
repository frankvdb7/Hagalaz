using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Bows
{
    /// <summary>
    ///     Class for bow related methods.
    /// </summary>
    public static class Bows
    {
        /// <summary>
        ///     Get's character bow type.
        ///     This method looks character equipment weapon slot.
        /// </summary>
        /// <returns></returns>
        public static BowType GetBowType(ICharacter character)
        {
            var weapon = character.Equipment[EquipmentSlot.Weapon];
            if (weapon == null)
            {
                return BowType.None;
            }

            var itemID = weapon.Id;
            switch (itemID)
            {
                case 839: return BowType.LongBow;
                case 841: return BowType.ShortBow;
                case 843: return BowType.OakShortBow;
                case 845: return BowType.OakLongBow;
                case 847: return BowType.WillowLongBow;
                case 849: return BowType.WillowShortBow;
                case 851:
                case 13523:
                    return BowType.MapleLongBow;
                case 853:
                case 13524:
                    return BowType.MapleShortBow;
                case 855:
                case 13525:
                    return BowType.YewLongBow;
                case 857:
                case 13526:
                    return BowType.YewShortBow;
                case 859:
                case 13527:
                    return BowType.MagicLongBow;
                case 861:
                case 13528:
                    return BowType.MagicShortBow;

                case 2883: return BowType.OgreBow;
                case 4827: return BowType.OgreCompositeBow;
                case 9705: return BowType.TrainingBow;
                case 10280:
                case 13541:
                    return BowType.WillowCompositeBow;
                case 10282:
                case 13542:
                    return BowType.YewCompositeBow;
                case 10284:
                case 13543:
                    return BowType.MagicCompositeBow;
            }

            if (itemID == 4212 || itemID >= 4214 && itemID <= 4223)
            {
                return BowType.CrystalBow;
            }

            if (itemID == 11235 || itemID == 13405 || itemID >= 15701 && itemID <= 15704)
            {
                return BowType.DarkBow;
            }

            switch (itemID)
            {
                case 14121: return BowType.SacredClayBow;
                case 16317: return BowType.TangleGumLongBow;
                case 16319: return BowType.SleepingElmLongBow;
                case 16321: return BowType.BloodSpindleLongBow;
                case 16323: return BowType.UtukuLongBow;
                case 16325: return BowType.SpineBeamLongBow;
                case 16327: return BowType.BoviStranglerLongBow;
                case 16329: return BowType.ThigatLongBow;
                case 16331: return BowType.CorpseThornLongBow;
                case 16333: return BowType.EntGallowLongBow;
                case 16335: return BowType.GraveCreeperLongBow;
                case 16337: return BowType.SagittarianLongBow;
                case 16867: return BowType.TangleGumShortBow;
                case 16869: return BowType.SleepingElmShortBow;
                case 16871: return BowType.BloodSpindleShortBow;
                case 16873: return BowType.UtukuShortBow;
                case 16875: return BowType.SpineBeamShortBow;
                case 16877: return BowType.BoviStranglerShortBow;
                case 16879: return BowType.ThigatShortBow;
                case 16881: return BowType.CorpseThornShortBow;
                case 16883: return BowType.EntGallowShortBow;
                case 16885: return BowType.GraveCreeperShortBow;
                case 16887: return BowType.SagittarianShortBow;
                case 16975: return BowType.SpineBeamShortBow;
                case 17295:
                case 21332:
                    return BowType.HexHunterBow;
                case 18331: return BowType.MapleLongBowSighted;
                case 18332: return BowType.MagicLongBowSighted;
                case 19143:
                case 19145:
                    return BowType.SaradominBow;
                case 19146:
                case 19148:
                    return BowType.GuthixBow;
                case 19149:
                case 19151:
                    return BowType.ZamorakBow;
                case 20171:
                case 20173:
                    return BowType.ZaryteBow;
                default: return BowType.None;
            }
        }

        /// <summary>
        ///     Get's suitable arrow types for given bow.
        /// </summary>
        /// <returns></returns>
        public static ArrowType[] GetSuitableArrowTypes(BowType bow)
        {
            switch (bow)
            {
                case BowType.TrainingBow: return [ArrowType.Training];
                case BowType.LongBow:
                case BowType.ShortBow:
                    return [ArrowType.Bronze, ArrowType.Iron];
                case BowType.OakLongBow:
                case BowType.OakShortBow:
                    return [ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel];
                case BowType.WillowLongBow:
                case BowType.WillowShortBow:
                    return [ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel, ArrowType.Mithril];
                case BowType.MapleLongBow:
                case BowType.MapleShortBow:
                case BowType.MapleLongBowSighted:
                    return [ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel, ArrowType.Mithril, ArrowType.Adamantine];
                case BowType.YewLongBow:
                case BowType.YewShortBow:
                case BowType.YewCompositeBow:
                    return [ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel, ArrowType.Mithril, ArrowType.Adamantine, ArrowType.Rune, ArrowType.Ice];
                case BowType.MagicLongBow:
                case BowType.MagicShortBow:
                case BowType.MagicCompositeBow:
                case BowType.MagicLongBowSighted:
                case BowType.SaradominBow:
                case BowType.GuthixBow:
                case BowType.ZamorakBow:
                case BowType.Seercull:
                    return
                    [
                        ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel, ArrowType.Mithril, ArrowType.Adamantine, ArrowType.Rune, ArrowType.Ice,
                        ArrowType.Saradomin, ArrowType.Guthix, ArrowType.Zamorak
                    ];
                case BowType.DarkBow:
                    return
                    [
                        ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel, ArrowType.Mithril, ArrowType.Adamantine, ArrowType.Rune, ArrowType.Dragon,
                        ArrowType.Saradomin, ArrowType.Guthix, ArrowType.Zamorak
                    ];
                case BowType.OgreCompositeBow:
                case BowType.OgreBow:
                    return
                    [
                        ArrowType.Ogre, ArrowType.Bronze, ArrowType.Iron, ArrowType.Steel, ArrowType.Mithril, ArrowType.Adamantine, ArrowType.Rune,
                        ArrowType.BrutalBronze, ArrowType.BrutalIron, ArrowType.BrutalSteel, ArrowType.BrutalBlack, ArrowType.BrutalMithril,
                        ArrowType.BrutalAdamantine, ArrowType.BrutalRune
                    ];
                case BowType.ZaryteBow: return [ArrowType.ZaryteBowInfinite];
                case BowType.CrystalBow: return [ArrowType.CrystalBowInfinite];
                default: return [];
            }
        }
    }
}