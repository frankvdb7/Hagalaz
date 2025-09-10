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
    [EquipmentScriptMetaData([21736, 21738, 21739, 21740, 21741, 21744, 21746, 21747, 21748, 21749, 21752, 21754, 21755, 21756, 21757, 21760, 21762, 21763, 21764, 21765])]
    public class AkrisaeEquipment : DegradingEquipment
    {
        /// <summary>
        ///     The dharok degraded equipment
        /// </summary>
        private static readonly int[] _akrisaeIDs =
        [
            21736, 21738, 21739, 21740, 21741, // akrisae hood
            21744, 21746, 21747, 21748, 21749, // akrisae mace
            21752, 21754, 21755, 21756, 21757, // akrisae top
            21760, 21762, 21763, 21764, 21765 // akrisae skirt
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
            for (var i = 0; i < _akrisaeIDs.Length; i++)
            {
                if (legs.Id == _akrisaeIDs[i])
                {
                    hasLegs = true;
                }
                else if (chest.Id == _akrisaeIDs[i])
                {
                    hasChest = true;
                }
                else if (weapon.Id == _akrisaeIDs[i])
                {
                    hasWeapon = true;
                }
                else if (hat.Id == _akrisaeIDs[i])
                {
                    hasHat = true;
                }
            }

            if (!hasLegs || !hasChest || !hasWeapon || !hasHat)
            {
                return;
            }

            character.AddState(new State(StateType.AkrisaeDoom, int.MaxValue));
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.AkrisaeDoom);

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == _akrisaeIDs[0] || item.Id == _akrisaeIDs[5]
                                         || item.Id == _akrisaeIDs[10] || item.Id == _akrisaeIDs[15])
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
            if (item.Id == _akrisaeIDs[0] || item.Id == _akrisaeIDs[5]
                                         || item.Id == _akrisaeIDs[10] || item.Id == _akrisaeIDs[15])
            {
                return item.Id + 2; // Degrade to 100 barrow item
            }

            return item.Id + 1;
        }
    }
}