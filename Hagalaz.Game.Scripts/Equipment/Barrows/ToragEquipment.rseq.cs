using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4745, 4952, 4953, 4954, 4955, 4747, 4958, 4959, 4960, 4961, 4749, 4964, 4965, 4966, 4967, 4751, 4970, 4971, 4972, 4973])]
    public class ToragEquipment : DegradingEquipment
    {
        /// <summary>
        ///     The torak equipment
        /// </summary>
        private static readonly int[] _toragIDs =
        [
            4745, 4952, 4953, 4954, 4955, // torag helm
            4747, 4958, 4959, 4960, 4961, // torag hammer
            4749, 4964, 4965, 4966, 4967, // torag body
            4751, 4970, 4971, 4972, 4973 // torag legs
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
            foreach (var itemId in _toragIDs)
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

            character.AddState(new ToragCorruptState());
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<ToragCorruptState>();

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == _toragIDs[0] || item.Id == _toragIDs[5]
                                       || item.Id == _toragIDs[10] || item.Id == _toragIDs[15])
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
            if (item.Id == _toragIDs[0])
            {
                return item.Id + 207;
            }

            if (item.Id == _toragIDs[5])
            {
                return item.Id + 211;
            }

            if (item.Id == _toragIDs[10])
            {
                return item.Id + 215;
            }

            if (item.Id == _toragIDs[15])
            {
                return item.Id + 219;
            }

            return item.Id + 1;
        }
    }
}