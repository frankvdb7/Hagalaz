using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Rings
{
    [ItemScriptMetaData([13281, 13282, 13283, 13284, 13285, 13286, 13287, 13288])]
    public class RingOfSlaying : ItemScript
    {
        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option3Click)
            {
                SendAssignedMessage(character);
            }
            else if (clickType == ComponentClickType.Option7Click)
            {
                character.Widgets.OpenDialogue(new RingOfSlayingDialogue(character.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(), false),
                    true,
                    item);
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }

        /// <summary>
        ///     Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option2Click)
            {
                character.Widgets.OpenDialogue(new RingOfSlayingDialogue(character.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(), true),
                    true,
                    item);
            }
            else if (clickType == ComponentClickType.Option3Click)
            {
                SendAssignedMessage(character);
            }
            else
            {
                base.ItemClickedInEquipment(clickType, item, character);
            }
        }

        private static void SendAssignedMessage(ICharacter character)
        {
            if (character.HasSlayerTask())
            {
                character.QueueTask(async () =>
                {
                    var slayer = character.Slayer;
                    var service = character.ServiceProvider.GetRequiredService<ISlayerService>();
                    var task = await service.FindSlayerTaskDefinition(slayer.CurrentTaskId);
                    if (task == null)
                    {
                        character.SendChatMessage("There is no task assigned to you yet.");
                        return;
                    }

                    character.SendChatMessage("You are currently assigned to kill: " + task.Name + ". Only " + slayer.CurrentKillCount + " more to go.");
                });
            }
            else
            {
                character.SendChatMessage("There is no task assigned to you yet.");
            }
        }
    }
}