using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Rings
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([2572, 20653, 20655, 20657, 20659])]
    public class RingOfWealth : EquipmentScript
    {
        /// <summary>
        ///     Happens when this item is equiped by specific character.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.RingOfWealthEquiped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.RingOfWealthEquiped);
    }
}