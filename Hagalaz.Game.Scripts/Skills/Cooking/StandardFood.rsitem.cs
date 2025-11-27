using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Cooking
{
    /// <summary>
    ///     Standard food item script.
    /// </summary>
    public class StandardFood : ItemScript
    {
        private readonly ICookingService _cookingService;
        private readonly IItemBuilder _itemBuilder;

        public StandardFood(ICookingService cookingService, IItemBuilder itemBuilder)
        {
            _cookingService = cookingService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Happens when character clicks specific item in inventory.
        /// </summary>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType != ComponentClickType.LeftClick)
            {
                base.ItemClickedInInventory(clickType, item, character);
                return;
            }

            if (character.EventManager.SendEvent(new EatAllowEvent(character, item)))
            {
                if (character.HasState<StunState>() || character.HasState<EatingState>())
                {
                    return;
                }

                character.Interrupt(this);
                character.QueueTask(() => EatFood(character, item));
            }
        }

        /// <summary>
        ///     Eats the food.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        public async Task EatFood(ICharacter character, IItem item)
        {
            var definition = await _cookingService.FindFoodById(item.Id);
            if (definition == null)
            {
                return;
            }

            var slot = character.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return;
            }

            if (definition.LeftItemId != -1)
            {
                character.Inventory.Replace(slot, _itemBuilder.Create().WithId(definition.LeftItemId).Build());
            }
            else
            {
                character.Inventory.Remove(item, slot);
            }

            character.AddState(new EatingState { TicksLeft = definition.EatingTime - 1, OnRemovedCallback = () => character.SendChatMessage("It restores some life points.") });
            character.QueueAnimation(Animation.Create(829));
            character.SendChatMessage("You eat the " + item.ItemDefinition.Name + ".");
            var maxAmount = character.Statistics.GetMaximumLifePoints();
            if (definition.ItemId == 15272)
            {
                maxAmount += 100;
            }

            character.Statistics.HealLifePoints(definition.HealAmount, maxAmount);
        }
    }
}