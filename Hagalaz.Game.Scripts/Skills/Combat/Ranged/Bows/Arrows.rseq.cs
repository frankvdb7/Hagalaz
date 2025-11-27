using System.Linq;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Bows
{
    /// <summary>
    ///     Class for misc methods related to arrows and arrows equipment script.
    /// </summary>
    [EquipmentScriptMetaData([882, 883, 5616, 5622, 884, 885, 5617, 5623, 886, 887, 5618, 5624, 888, 889, 5619, 5625, 890, 891, 5620, 5626, 892, 893, 5621, 5627, 11212, 11227, 11228, 11229, 598, 942, 2532, 2533, 2534, 2535, 2536, 2537, 2538, 2539, 2540, 2541, 11217, 2865, 2866, 4773, 4778, 4783, 4788, 4793, 4798, 4803, 19152, 19157, 19162, 78, 9706, 4160])]
    public class Arrows : EquipmentScript
    {
        /// <summary>
        ///     Bronze arrow ids.
        /// </summary>
        public static readonly int[] Bronze =
        [
            882, 883, 5616, 5622
        ];

        /// <summary>
        ///     Iron arrows ids.
        /// </summary>
        public static readonly int[] Iron =
        [
            884, 885, 5617, 5623
        ];

        /// <summary>
        ///     Steel arrows ids.
        /// </summary>
        public static readonly int[] Steel =
        [
            886, 887, 5618, 5624
        ];

        /// <summary>
        ///     Mithril arrows ids.
        /// </summary>
        public static readonly int[] Mithril =
        [
            888, 889, 5619, 5625
        ];

        /// <summary>
        ///     Adamant arrows ids.
        /// </summary>
        public static readonly int[] Adamant =
        [
            890, 891, 5620, 5626
        ];

        /// <summary>
        ///     Rune arrows ids.
        /// </summary>
        public static readonly int[] Rune =
        [
            892, 893, 5621, 5627
        ];

        /// <summary>
        ///     Dragon arrows ids.
        /// </summary>
        public static readonly int[] Dragon =
        [
            11212, 11227, 11228, 11229
        ];

        /// <summary>
        ///     Bronze brutal arrow id.
        /// </summary>
        public static readonly int BronzeBrutal = 4773;

        /// <summary>
        ///     Iron brutal arrow id.
        /// </summary>
        public static readonly int IronBrutal = 4778;

        /// <summary>
        ///     Steel brutal arrow id.
        /// </summary>
        public static readonly int SteelBrutal = 4783;

        /// <summary>
        ///     Black brutal arrow id.
        /// </summary>
        public static readonly int BlackBrutal = 4788;

        /// <summary>
        ///     Mithril brutal arrow id.
        /// </summary>
        public static readonly int MithrilBrutal = 4793;

        /// <summary>
        ///     Adamant brutal arrow id.
        /// </summary>
        public static readonly int AdamantBrutal = 4798;

        /// <summary>
        ///     Rune brutal arrow id.
        /// </summary>
        public static readonly int RuneBrutal = 4803;

        /// <summary>
        ///     Bronze fire arrows ids.
        /// </summary>
        public static readonly int[] BronzeFire =
        [
            598, 942
        ];

        /// <summary>
        ///     Iron fire arrows ids.
        /// </summary>
        public static readonly int[] IronFire =
        [
            2532, 2533
        ];

        /// <summary>
        ///     Steel fire arrows ids.
        /// </summary>
        public static readonly int[] SteelFire =
        [
            2534, 2535
        ];

        /// <summary>
        ///     Mithril fire arrows ids.
        /// </summary>
        public static readonly int[] MithrilFire =
        [
            2536, 2537
        ];

        /// <summary>
        ///     Adamant fire arrows ids.
        /// </summary>
        public static readonly int[] AdamantFire =
        [
            2538, 2539
        ];

        /// <summary>
        ///     Rune fire arrows ids.
        /// </summary>
        public static readonly int[] RuneFire =
        [
            2540, 2541
        ];

        /// <summary>
        ///     Dragon fire arrows ids.
        /// </summary>
        public static readonly int[] DragonFire =
        [
            11217
        ];

        /// <summary>
        ///     Saradomin arrow id.
        /// </summary>
        public static readonly int Saradomin = 19152;

        /// <summary>
        ///     Guthix arrow id.
        /// </summary>
        public static readonly int Guthix = 19157;

        /// <summary>
        ///     Zamorak arrow id.
        /// </summary>
        public static readonly int Zamorak = 19162;

        /// <summary>
        ///     Ice arrow id.
        /// </summary>
        public static readonly int Ice = 78;

        /// <summary>
        ///     Ogre arrows ids.
        /// </summary>
        public static readonly int[] Ogre =
        [
            2865, 2866
        ];

        /// <summary>
        ///     Training arrows ids.
        /// </summary>
        public static readonly int Training = 9706;

        /// <summary>
        ///     Broad arrows ids.
        /// </summary>
        public static readonly int Broad = 4160;

        /// <summary>
        ///     Gets the arrow pull back graphic.
        /// </summary>
        /// <returns></returns>
        public static int GetArrowPullBackGraphic(ArrowType arrow)
        {
            switch (arrow)
            {
                case ArrowType.Bronze: return 19;
                case ArrowType.Iron: return 18;
                case ArrowType.Steel: return 20;
                case ArrowType.Mithril: return 21;
                case ArrowType.Adamantine: return 22;
                case ArrowType.Rune: return 24;
                case ArrowType.FireBronze:
                case ArrowType.FireIron:
                case ArrowType.FireSteel:
                case ArrowType.FireMithril:
                case ArrowType.FireAdamantine:
                case ArrowType.FireRune:
                case ArrowType.FireDragon:
                    return 26;
                case ArrowType.Dragon: return 1116;
                case ArrowType.Ice: return 25;
                case ArrowType.CrystalBowInfinite: return 250;
                case ArrowType.ZaryteBowInfinite: return 2962;
                default: return -1;
            }
        }

        /// <summary>
        ///     Gets the arrow graphic.
        /// </summary>
        /// <param name="arrow">The arrow.</param>
        /// <returns></returns>
        public static int GetArrowGraphic(ArrowType arrow)
        {
            switch (arrow)
            {
                case ArrowType.Bronze: return 10;
                case ArrowType.Iron: return 9;
                case ArrowType.Steel: return 11;
                case ArrowType.Mithril: return 12;
                case ArrowType.Adamantine: return 13;
                case ArrowType.Rune: return 15;
                case ArrowType.Ice: return 16;
                case ArrowType.FireBronze:
                case ArrowType.FireIron:
                case ArrowType.FireSteel:
                case ArrowType.FireMithril:
                case ArrowType.FireAdamantine:
                case ArrowType.FireRune:
                case ArrowType.FireDragon:
                    return 17;
                case ArrowType.Dragon: return 1115;
                case ArrowType.CrystalBowInfinite: return 249;
                case ArrowType.ZaryteBowInfinite: return 1066;
                default: return -1;
            }
        }

        /// <summary>
        ///     Get's character arrow type.
        ///     This method looks character equipment ammo slot.
        /// </summary>
        /// <returns></returns>
        public static ArrowType
            GetArrowType(ICharacter character)
        {
            var ammo = character.Equipment[EquipmentSlot.Arrow];
            var bow = Bows.GetBowType(character);
            switch (bow)
            {
                case BowType.ZaryteBow: return ArrowType.ZaryteBowInfinite;
                case BowType.CrystalBow: return ArrowType.CrystalBowInfinite;
            }

            if (ammo == null)
            {
                return ArrowType.None;
            }

            var itemID = ammo.Id;
            if (Lookup(itemID, Bronze))
            {
                return ArrowType.Bronze;
            }

            if (Lookup(itemID, Iron))
            {
                return ArrowType.Iron;
            }

            if (Lookup(itemID, Steel))
            {
                return ArrowType.Steel;
            }

            if (Lookup(itemID, Mithril))
            {
                return ArrowType.Mithril;
            }

            if (Lookup(itemID, Adamant))
            {
                return ArrowType.Adamantine;
            }

            if (Lookup(itemID, Rune))
            {
                return ArrowType.Rune;
            }

            if (Lookup(itemID, Dragon))
            {
                return ArrowType.Dragon;
            }

            if (Lookup(itemID, BronzeFire))
            {
                return ArrowType.FireBronze;
            }

            if (Lookup(itemID, IronFire))
            {
                return ArrowType.FireIron;
            }

            if (Lookup(itemID, SteelFire))
            {
                return ArrowType.FireSteel;
            }

            if (Lookup(itemID, MithrilFire))
            {
                return ArrowType.FireMithril;
            }

            if (Lookup(itemID, AdamantFire))
            {
                return ArrowType.FireAdamantine;
            }

            if (Lookup(itemID, RuneFire))
            {
                return ArrowType.FireRune;
            }

            if (Lookup(itemID, DragonFire))
            {
                return ArrowType.FireDragon;
            }

            if (Lookup(itemID, Ogre))
            {
                return ArrowType.Ogre;
            }

            if (itemID == BronzeBrutal)
            {
                return ArrowType.BrutalBronze;
            }

            if (itemID == IronBrutal)
            {
                return ArrowType.BrutalIron;
            }

            if (itemID == SteelBrutal)
            {
                return ArrowType.BrutalSteel;
            }

            if (itemID == MithrilBrutal)
            {
                return ArrowType.BrutalMithril;
            }

            if (itemID == AdamantBrutal)
            {
                return ArrowType.BrutalAdamantine;
            }

            if (itemID == RuneBrutal)
            {
                return ArrowType.BrutalRune;
            }

            if (itemID == Saradomin)
            {
                return ArrowType.Saradomin;
            }

            if (itemID == Guthix)
            {
                return ArrowType.Guthix;
            }

            if (itemID == Zamorak)
            {
                return ArrowType.Zamorak;
            }

            if (itemID == Ice)
            {
                return ArrowType.Ice;
            }

            if (itemID == Training)
            {
                return ArrowType.Training;
            }

            if (itemID == Broad)
            {
                return ArrowType.Broad;
            }

            return ArrowType.None;
        }

        /// <summary>
        ///     Checks if array contains given value.
        /// </summary>
        private static bool Lookup(int v, int[] array) => array.Any(t => t == v);

        /// <summary>
        ///     Make's one array from given arrays.
        /// </summary>
        /// <returns></returns>
        private static int[] MakeArray(params int[][] arrays)
        {
            var total = arrays.Sum(t => t.Length);

            var array = new int[total];
            total = 0;
            foreach (var t in arrays)
                foreach (var t1 in t)
                {
                    array[total++] = t1;
                }

            return array;
        }

        /// <summary>
        ///     Happens when arrows are equipped for this character.
        /// </summary>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (Lookup(item.Id, Dragon))
            {
                character.AddState(new DragonArrowsEquippedState());
            }

            character.AddState(new ArrowsEquippedState());
        }

        /// <summary>
        ///     Happens when arrows are unequipped for this character.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="character"></param>
        public override void OnUnequipped(IItem item, ICharacter character)
        {
            if (Lookup(item.Id, Dragon))
            {
                character.RemoveState<DragonArrowsEquippedState>();
            }

            character.RemoveState<ArrowsEquippedState>();
        }

        /// <summary>
        ///     Get's items suitable for this equipment script.
        /// </summary>
        /// <returns></returns>
    }
}