using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Rings
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([2572, 20653, 20655, 20657, 20659])]
    public class RingOfWealth : EquipmentScript
    {
        /// <summary>
        ///     Happens when this item is equipped by specific character.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character) => character.AddState(new RingOfWealthEquippedState());

        /// <summary>
        ///     Happens when this item is unequipped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<RingOfWealthEquippedState>();
    }
}