using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Auras
{
    [ItemScriptMetaData([23880, 23882, 23884, 23886, 23888, 23890, 23892, 23894])]
    public class CosmeticAuraActivated : ItemScript
    {
        /// <summary>
        ///     Items the clicked in equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="aura">The aura.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem aura, ICharacter character)
        {
            if (CosmeticAuraOperations.TryHandleEquipmentClick(clickType, aura, character, 16)) return;
            base.ItemClickedInEquipment(clickType, aura, character);
        }

        /// <summary>
        ///     Toggles the aura.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public void ToggleAura(ICharacter character, IItem aura)
        {
            CosmeticAuraOperations.ToggleAura(character, aura, 16);
        }
    }
}
