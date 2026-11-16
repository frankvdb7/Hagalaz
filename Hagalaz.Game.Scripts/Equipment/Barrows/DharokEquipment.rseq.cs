using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4716, 4880, 4881, 4882, 4883, 4718, 4886, 4887, 4888, 4889, 4720, 4892, 4893, 4894, 4895, 4722, 4898, 4899, 4900, 4901])]
    public class DharokEquipment : BarrowsEquipment
    {
        private static readonly int[] _dharokIDs =
        [
            4716, 4880, 4881, 4882, 4883, // dharok helm
            4718, 4886, 4887, 4888, 4889, // dharok axe
            4720, 4892, 4893, 4894, 4895, // dharok body
            4722, 4898, 4899, 4900, 4901 // dharok legs
        ];

        protected override ReadOnlySpan<int> ItemIds => _dharokIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new DharokWretchedStrengthState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<DharokWretchedStrengthState>();
    }
}
