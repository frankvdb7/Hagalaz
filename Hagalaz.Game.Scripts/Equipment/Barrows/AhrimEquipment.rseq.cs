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
    public class AhrimEquipment : DegradingEquipment
    {
        /// <summary>
        ///     The dharok degraded equipment
        /// </summary>
        private static readonly int[] _ahrimIDs =
        [
            4708, 4856, 4857, 4858, 4859, // ahrim hood
            4710, 4862, 4863, 4864, 4865, // ahirm staff
            4712, 4868, 4869, 4870, 4871, // ahrim top
            4714, 4874, 4875, 4876, 4877 // ahrim legs
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
            foreach (var itemId in _ahrimIDs)
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

            character.AddState(new State(StateType.AhrimBlight, int.MaxValue));
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.AhrimBlight);

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int> GetSuitableItems() => _ahrimIDs;

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == _ahrimIDs[0] || item.Id == _ahrimIDs[5]
                                       || item.Id == _ahrimIDs[10] || item.Id == _ahrimIDs[15])
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
            if (item.Id == _ahrimIDs[0])
            {
                return (short)(item.Id + 148);
            }

            if (item.Id == _ahrimIDs[5])
            {
                return (short)(item.Id + 152);
            }

            if (item.Id == _ahrimIDs[10])
            {
                return (short)(item.Id + 156);
            }

            if (item.Id == _ahrimIDs[15])
            {
                return (short)(item.Id + 160);
            }

            return (short)(item.Id + 1);
        }
    }
}