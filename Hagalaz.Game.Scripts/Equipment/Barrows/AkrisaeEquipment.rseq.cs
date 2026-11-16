using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([21736, 21738, 21739, 21740, 21741, 21744, 21746, 21747, 21748, 21749, 21752, 21754, 21755, 21756, 21757, 21760, 21762, 21763, 21764, 21765])]
    public class AkrisaeEquipment : BarrowsEquipment
    {
        private static readonly int[] _akrisaeIDs =
        [
            21736, 21738, 21739, 21740, 21741, // akrisae hood
            21744, 21746, 21747, 21748, 21749, // akrisae mace
            21752, 21754, 21755, 21756, 21757, // akrisae top
            21760, 21762, 21763, 21764, 21765 // akrisae skirt
        ];

        protected override ReadOnlySpan<int> ItemIds => _akrisaeIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new AkrisaeDoomState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<AkrisaeDoomState>();
    }
}
