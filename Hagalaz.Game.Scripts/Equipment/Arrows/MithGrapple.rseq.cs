using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows;

namespace Hagalaz.Game.Scripts.Equipment.Arrows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([9419])]
    public class MithGrapple : EquipmentScript
    {
        /// <summary>
        ///     Happens when this item is equiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new MithGrappleEquipedState());

        /// <summary>
        ///     Happens when this item is unequiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<MithGrappleEquipedState>();
    }
}