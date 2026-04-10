using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Prayer
{
    public static class Prayer
    {
        public static bool UseOnAltar(ICharacter character, IItem item, IGameObject altar, IPrayerService prayerService)
        {
            if (character.HasState<BuryingBonesState>())
            {
                return true;
            }

            var slot = character.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return false;
            }
            
            var dto = prayerService.FindById(item.Id).Result;
            if (dto == null)
            {
                return false;
            }
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = [dto.ItemId];
            dialogue.Options = InteractiveDialogueOptions.Offer;
            dialogue.Info = "Choose how many you wish to offer, <br>then click on the item to begin.";
            dialogue.PerformMakeProductCallback = (_, currentCount) =>
            {
                if (currentCount > 0)
                {
                    character.QueueTask(new OfferTask(character, altar, dto, currentCount, 3));
                }

                return true;
            };

            var count = character.Inventory.GetCountById(dto.ItemId);
            dialogue.SetMaxCount(count, false);
            dialogue.SetCurrentCount(count, false);

            return InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }
    }
}