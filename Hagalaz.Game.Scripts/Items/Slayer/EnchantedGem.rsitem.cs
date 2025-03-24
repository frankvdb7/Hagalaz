using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Slayer
{
    [ItemScriptMetaData([4155])]
    public class EnchantedGem : ItemScript
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
                var dialogue = character.ServiceProvider.GetRequiredService<EnchantedGemDialogue>();
                character.Widgets.OpenDialogue(dialogue, true, item);
            }
            else if (clickType == ComponentClickType.Option2Click)
            {
                character.QueueTask(async () =>
                {
                    if (character.HasSlayerTask())
                    {
                        var slayerService = character.ServiceProvider.GetRequiredService<ISlayerService>();
                        var slayerTask = await slayerService.FindSlayerTaskDefinition(character.Slayer.CurrentTaskId);
                        var slayer = character.Slayer;
                        character.SendChatMessage("You are currently assigned to kill: " + slayerTask!.Name + ". Only " + slayer.CurrentKillCount +
                                                  " more to go.");
                    }
                    else
                    {
                        character.SendChatMessage("There is no task assigned to you yet.");
                    }
                });
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }
    }
}