using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Shields
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([1540])]
    public class AntiDragonShield : EquipmentScript
    {
        /// <summary>
        ///     Called when [equipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnEquipped(IItem item, ICharacter character) => character.AddState(new AntiDragonfireShieldState());

        /// <summary>
        ///     Called when [unequipped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<AntiDragonfireShieldState>();
    }
}