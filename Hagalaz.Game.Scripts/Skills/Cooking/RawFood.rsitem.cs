using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Cooking
{
    /// <summary>
    /// </summary>
    public class RawFood : ItemScript
    {
        private readonly ICookingService _cookingService;

        public RawFood(ICookingService cookingService)
        {
            _cookingService = cookingService;
        }

        /// <summary>
        ///     Uses the item on game object.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, IGameObject usedOn, ICharacter character)
        {
            if (usedOn.Name.ToLower().Equals("stove") || usedOn.Name.ToLower().Equals("fire") || usedOn.Name.ToLower().Equals("range"))
            {
                return TryCookFood(character, usedOn, used);
            }

            return false;
        }

        public bool TryCookFood(ICharacter character, IGameObject obj, IItem item)
        {
            var definition = _cookingService.FindRawFoodById(item.Id).Result;
            if (definition == null)
            {
                return false;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Cooking) < definition.RequiredLevel)
            {
                character.SendChatMessage("You do not have the required cooking level to cook this food.");
                return false;
            }

            if (character.Inventory.GetInstanceSlot(item) == -1)
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
            var cookingDialogue = character.ServiceProvider.GetRequiredService<CookingDialogue>();
            cookingDialogue.Obj = obj;
            cookingDialogue.Dto = definition;
            character.Widgets.OpenWidget((short)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, cookingDialogue, false);
            return true;
        }
    }
}