using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([11702, 11704, 11706, 11708])]
    public class GodSwordHilt : ItemScript
    {
        /// <summary>
        ///     Uses the item on an other item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            if (used.Id == 11702 && usedWith.Id == 11690 || usedWith.Id == 11702 && used.Id == 11690)
            {
                // Armadyl Godsword
                var usedSlot = character.Inventory.GetInstanceSlot(used);
                if (usedSlot == -1)
                {
                    return false;
                }

                var usedWithSlot = character.Inventory.GetInstanceSlot(usedWith);
                if (usedWithSlot == -1)
                {
                    return false;
                }

                character.Inventory.Remove(used, usedSlot);
                character.Inventory.Replace(usedWithSlot, new Item(11694, 1));
                return true;
            }

            if (used.Id == 11708 && usedWith.Id == 11690 || usedWith.Id == 11708 && used.Id == 11690)
            {
                // Zamorak Godsword
                var usedSlot = character.Inventory.GetInstanceSlot(used);
                if (usedSlot == -1)
                {
                    return false;
                }

                var usedWithSlot = character.Inventory.GetInstanceSlot(usedWith);
                if (usedWithSlot == -1)
                {
                    return false;
                }

                character.Inventory.Remove(used, usedSlot);
                character.Inventory.Replace(usedWithSlot, new Item(11700, 1));
                return true;
            }

            if (used.Id == 11706 && usedWith.Id == 11690 || usedWith.Id == 11706 && used.Id == 11690)
            {
                // Saradomin Godsword
                var usedSlot = character.Inventory.GetInstanceSlot(used);
                if (usedSlot == -1)
                {
                    return false;
                }

                var usedWithSlot = character.Inventory.GetInstanceSlot(usedWith);
                if (usedWithSlot == -1)
                {
                    return false;
                }

                character.Inventory.Remove(used, usedSlot);
                character.Inventory.Replace(usedWithSlot, new Item(11698, 1));
                return true;
            }

            if (used.Id == 11704 && usedWith.Id == 11690 || usedWith.Id == 11704 && used.Id == 11690)
            {
                // Bandos Godsword
                var usedSlot = character.Inventory.GetInstanceSlot(used);
                if (usedSlot == -1)
                {
                    return false;
                }

                var usedWithSlot = character.Inventory.GetInstanceSlot(usedWith);
                if (usedWithSlot == -1)
                {
                    return false;
                }

                character.Inventory.Remove(used, usedSlot);
                character.Inventory.Replace(usedWithSlot, new Item(11696, 1));
                return true;
            }

            return false;
        }
    }
}