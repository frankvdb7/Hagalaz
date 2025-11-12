using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Shields
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([1540])]
    public class AntiDragonShield : EquipmentScript
    {
        /// <summary>
        ///     Called when [equiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new AntiDragonfireShieldState());

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<AntiDragonfireShieldState>();
    }
}