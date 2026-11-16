using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4708, 4856, 4857, 4858, 4859, 4710, 4862, 4863, 4864, 4865, 4712, 4868, 4869, 4870, 4871, 4714, 4874, 4875, 4876, 4877])]
    public class AhrimEquipment : BarrowsEquipment
    {
        private static readonly int[] _ahrimIDs =
        [
            4708, 4856, 4857, 4858, 4859, // ahrim hood
            4710, 4862, 4863, 4864, 4865, // ahrim staff
            4712, 4868, 4869, 4870, 4871, // ahrim top
            4714, 4874, 4875, 4876, 4877 // ahrim legs
        ];

        protected override ReadOnlySpan<int> ItemIds => _ahrimIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new AhrimBlightState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<AhrimBlightState>();
    }
}
