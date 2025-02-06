using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([20428])]
    public class JmodBoss : ItemScript
    {
        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.LeftClick)
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.GameModerator))
                {
                    if (character.Appearance.NpcId == 2417 || character.Appearance.NpcId == 3334)
                    {
                        character.Appearance.TurnToPlayer();
                    }
                    else
                    {
                        character.Appearance.TurnToNpc(2417); // wildy wyrm burrowed
                    }

                    return;
                }
            }

            base.ItemClickedInInventory(clickType, item, character);
        }
    }
}