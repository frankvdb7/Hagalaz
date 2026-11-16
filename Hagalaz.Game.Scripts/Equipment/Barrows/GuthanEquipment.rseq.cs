using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    ///     Registers only the armour pieces; the war spear IDs are registered by <c>GuthansWarSpear</c>, which inherits this behavior.
    /// </summary>
    [EquipmentScriptMetaData([4724, 4904, 4905, 4906, 4907, 4728, 4916, 4917, 4918, 4919, 4730, 4922, 4923, 4924, 4925])]
    public class GuthanEquipment : BarrowsEquipment
    {
        private static readonly int[] _guthanIDs =
        [
            4724, 4904, 4905, 4906, 4907, // guthan helm
            4726, 4910, 4911, 4912, 4913, // guthan warspear
            4728, 4916, 4917, 4918, 4919, // guthan platebody
            4730, 4922, 4923, 4924, 4925 // guthan skirt
        ];

        protected override ReadOnlySpan<int> ItemIds => _guthanIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new GuthanInfestationState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<GuthanInfestationState>();
    }
}
