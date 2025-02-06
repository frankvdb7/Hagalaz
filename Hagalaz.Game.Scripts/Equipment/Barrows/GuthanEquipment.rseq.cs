using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    public class GuthanEquipment : DegradingEquipment
    {
        /// <summary>
        ///     The guthan equipment
        /// </summary>
        private static readonly int[] GuthanIDs =
        [
            4724, 4904, 4905, 4906, 4907, // guthan helm
            4726, 4910, 4911, 4912, 4913, // guthan warspear
            4728, 4916, 4917, 4918, 4919, // guthan platebody
            4730, 4922, 4923, 4924, 4925 // guthan skirt
        ];

        /// <summary>
        ///     Happens when this item is equiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            var legs = character.Equipment[EquipmentSlot.Legs];
            var chest = character.Equipment[EquipmentSlot.Chest];
            var weapon = character.Equipment[EquipmentSlot.Weapon];
            var hat = character.Equipment[EquipmentSlot.Hat];
            if (legs == null || chest == null || weapon == null || hat == null)
            {
                return;
            }

            var hasLegs = false;
            var hasChest = false;
            var hasWeapon = false;
            var hasHat = false;
            foreach (var itemId in GuthanIDs)
            {
                if (legs.Id == itemId)
                {
                    hasLegs = true;
                }
                else if (chest.Id == itemId)
                {
                    hasChest = true;
                }
                else if (weapon.Id == itemId)
                {
                    hasWeapon = true;
                }
                else if (hat.Id == itemId)
                {
                    hasHat = true;
                }
            }

            if (!hasLegs || !hasChest || !hasWeapon || !hasHat)
            {
                return;
            }

            character.AddState(new State(StateType.GuthanInfestation, int.MaxValue));
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.GuthanInfestation);

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int> GetSuitableItems()
        {
            var ids = new int[GuthanIDs.Length - 5];
            var offset = 0;
            for (var i = 0; i < GuthanIDs.Length; i++)
            {
                if (i >= 5 && i <= 9)
                {
                    offset = 5;
                    continue;
                }

                ids[i - offset] = GuthanIDs[i];
            }

            return ids;
        }

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == GuthanIDs[0] || item.Id == GuthanIDs[5]
                                        || item.Id == GuthanIDs[10] || item.Id == GuthanIDs[15])
            {
                return 0; // Degrade to 100 barrow item
            }

            return 22500; // 1/4 of 90.000
        }

        /// <summary>
        ///     Gets the degration on death factor.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override double GetDegrationOnDeathFactor(IItem item) => 0.80;

        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override short GetDegradedItemID(IItem item)
        {
            if (item.Id == GuthanIDs[0])
            {
                return (short)(item.Id + 180);
            }

            if (item.Id == GuthanIDs[5])
            {
                return (short)(item.Id + 184);
            }

            if (item.Id == GuthanIDs[10])
            {
                return (short)(item.Id + 188);
            }

            if (item.Id == GuthanIDs[15])
            {
                return (short)(item.Id + 192);
            }

            return (short)(item.Id + 1);
        }
    }
}