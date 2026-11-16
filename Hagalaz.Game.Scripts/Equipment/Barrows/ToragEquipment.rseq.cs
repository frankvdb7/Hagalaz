using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4745, 4952, 4953, 4954, 4955, 4747, 4958, 4959, 4960, 4961, 4749, 4964, 4965, 4966, 4967, 4751, 4970, 4971, 4972, 4973])]
    public class ToragEquipment : BarrowsEquipment
    {
        private static readonly int[] _toragIDs =
        [
            4745, 4952, 4953, 4954, 4955, // torag helm
            4747, 4958, 4959, 4960, 4961, // torag hammer
            4749, 4964, 4965, 4966, 4967, // torag body
            4751, 4970, 4971, 4972, 4973 // torag legs
        ];

        protected override ReadOnlySpan<int> ItemIds => _toragIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new ToragCorruptState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<ToragCorruptState>();
    }
}
