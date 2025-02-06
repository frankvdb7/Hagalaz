using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Tabs
{
    /// <summary>
    ///     Contains tele tab script.
    /// </summary>
    [ItemScriptMetaData([8007, 8008, 8009, 8010, 8011, 8013])]
    public class TeleTab : ItemScript
    {
        /// <summary>
        ///     Contains normal teleports.
        /// </summary>
        private static readonly Dictionary<int, TabTeleport> _tabTeleports = new();

        /// <summary>
        ///     Initializes the static <see cref="TeleTab" /> class.
        /// </summary>
        static TeleTab()
        {
            _tabTeleports.Add(8007, new TabTeleport(3213, 3428, 0, 5, 8007)); // Varrock tab
            _tabTeleports.Add(8008, new TabTeleport(3222, 3218, 0, 4, 8008)); // Lumbridge tab
            _tabTeleports.Add(8009, new TabTeleport(2965, 3380, 0, 4, 8009)); // Falador tab
            _tabTeleports.Add(8010, new TabTeleport(2757, 3478, 0, 3, 8010)); // Camelot tab
            _tabTeleports.Add(8011, new TabTeleport(2662, 3307, 0, 6, 8011)); // Ardougne tab
            _tabTeleports.Add(8013, new TabTeleport(3100, 3499, 0, 0, 8013)); // Home tab
        }

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
                var teleport = _tabTeleports[item.Id];
                if (teleport.CanTeleport(character))
                {
                    _tabTeleports[item.Id].PerformTeleport(character);
                    return;
                }
            }

            base.ItemClickedInInventory(clickType, item, character);
        }
    }
}