using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Prayer
{
    public static class Prayer
    {
        public static bool UseOnAltar(ICharacter character, IItem item, IGameObject altar, IPrayerService prayerService)
        {
            if (character.HasState(StateType.BuryingBones))
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
            var defaultScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.InteractiveChatBox, 0, defaultScript, false);
            var parent = character.Widgets.GetOpenWidget((short)DialogueInterfaces.InteractiveChatBox);
            if (parent == null)
            {
                return false;
            }

            var offerDialogue = character.ServiceProvider.GetRequiredService<OfferDialogue>();
            offerDialogue.Definition = dto;
            offerDialogue.Altar = altar;
            offerDialogue.TickDelay = 3;
            character.Widgets.OpenWidget((short)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, offerDialogue, false);
            return true;
        }
    }
}