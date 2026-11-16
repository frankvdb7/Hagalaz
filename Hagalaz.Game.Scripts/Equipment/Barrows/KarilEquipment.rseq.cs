using System;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    ///     Registers only the armour pieces; the crossbow IDs are registered by <c>KarilsCrossbow</c>, which inherits this behavior.
    /// </summary>
    [EquipmentScriptMetaData([4732, 4928, 4929, 4930, 4931, 4736, 4940, 4941, 4942, 4943, 4738, 4946, 4947, 4948, 4949])]
    public class KarilEquipment : BarrowsEquipment
    {
        private static readonly int[] _karilIDs =
        [
            4732, 4928, 4929, 4930, 4931, // karils coif
            4734, 4934, 4935, 4936, 4937, // karils crossbow
            4736, 4940, 4941, 4942, 4943, // karils top
            4738, 4946, 4947, 4948, 4949 // karils skirt
        ];

        protected override ReadOnlySpan<int> ItemIds => _karilIDs;

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            if (HasCompleteSet(character))
            {
                character.AddState(new KarilTaintState());
            }
        }

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<KarilTaintState>();
    }
}
