using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4753, 4976, 4977, 4978, 4979, 4755, 4982, 4983, 4984, 4985, 4757, 4988, 4989, 4990, 4991, 4759, 4994, 4995, 4996, 4997])]
    public class VeracEquipment : BarrowsEquipment
    {
        private static readonly int[] _veracIDs =
        [
            4753, 4976, 4977, 4978, 4979, // verac helm
            4755, 4982, 4983, 4984, 4985, // verac flail
            4757, 4988, 4989, 4990, 4991, // verac top
            4759, 4994, 4995, 4996, 4997 // verac skirt
        ];

        protected override ReadOnlySpan<int> ItemIds => _veracIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new VeracDefileState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<VeracDefileState>();
    }
}
