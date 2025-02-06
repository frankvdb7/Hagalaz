using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([11686, 11688, 11690, 11692, 11710, 11712, 11714])]
    public class GodSwordShard : ItemScript
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
            if (used.Id == 11710 && usedWith.Id == 11692 || usedWith.Id == 11710 && used.Id == 11692 ||
                used.Id == 11712 && usedWith.Id == 11688 || usedWith.Id == 11712 && used.Id == 11688 ||
                used.Id == 11714 && usedWith.Id == 11686 || usedWith.Id == 11714 && used.Id == 11686)
            {
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
                character.Inventory.Replace(usedWithSlot, new Item(11690, 1));
                return true;
            }

            if (used.Id == 11712 && usedWith.Id == 11714 || usedWith.Id == 11712 && used.Id == 11714)
            {
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
                character.Inventory.Replace(usedWithSlot, new Item(11692, 1));
                return true;
            }

            if (used.Id == 11710 && usedWith.Id == 11712 || usedWith.Id == 11710 && used.Id == 11712)
            {
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
                character.Inventory.Replace(usedWithSlot, new Item(11686, 1));
                return true;
            }

            if (used.Id == 11710 && usedWith.Id == 11714 || usedWith.Id == 11710 && usedWith.Id == 11714)
            {
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
                character.Inventory.Replace(usedWithSlot, new Item(11688, 1));
                return true;
            }

            return base.UseItemOnItem(used, usedWith, character);
        }
    }
}