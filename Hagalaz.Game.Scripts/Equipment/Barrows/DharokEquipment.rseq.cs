using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4716, 4880, 4881, 4882, 4883, 4718, 4886, 4887, 4888, 4889, 4720, 4892, 4893, 4894, 4895, 4722, 4898, 4899, 4900, 4901])]
    public class DharokEquipment : DegradingEquipment
    {
        /// <summary>
        ///     The dharok degraded equipment
        /// </summary>
        private static readonly int[] _dharokIDs =
        [
            4716, 4880, 4881, 4882, 4883, // dharok helm
            4718, 4886, 4887, 4888, 4889, // dharok axe
            4720, 4892, 4893, 4894, 4895, // dharok body
            4722, 4898, 4899, 4900, 4901 // dharok legs
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
            foreach (var itemId in _dharokIDs)
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

            character.AddState(new DharokWretchedStrengthState());
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<DharokWretchedStrengthState>();

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == _dharokIDs[0] || item.Id == _dharokIDs[5]
                                        || item.Id == _dharokIDs[10] || item.Id == _dharokIDs[15])
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
        public override int GetDegradedItemID(IItem item)
        {
            if (item.Id == _dharokIDs[0])
            {
                return item.Id + 164;
            }

            if (item.Id == _dharokIDs[5])
            {
                return item.Id + 168;
            }

            if (item.Id == _dharokIDs[10])
            {
                return item.Id + 172;
            }

            if (item.Id == _dharokIDs[15])
            {
                return item.Id + 176;
            }

            return item.Id + 1;
        }
    }
}