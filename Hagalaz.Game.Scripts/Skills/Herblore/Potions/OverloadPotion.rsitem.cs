using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Tasks;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData(itemIds: [15332, 15333, 15334, 15335])]
    public class OverloadPotion : Potion
    {
        private readonly IItemBuilder _itemBuilder;

        public OverloadPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IItemBuilder itemBuilder) : base(
            potionSkillService,
            herbloreSkillService) =>
            _itemBuilder = itemBuilder;

        /// <summary>
        ///     Happens when character clicks specific item in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType != ComponentClickType.LeftClick)
            {
                base.ItemClickedInInventory(clickType, item, character);
                return;
            }

            if (!character.EventManager.SendEvent(new DrinkAllowEvent(character, item)))
            {
                return;
            }

            if (character.HasState(StateType.Stun) || character.HasState(StateType.Drinking))
            {
                return;
            }

            if (character.Statistics.LifePoints <= 500)
            {
                character.SendChatMessage("It is wise not to drink this right now.");
                return;
            }

            if (character.HasState(StateType.OverloadEffect))
            {
                character.SendChatMessage("You cannot drink another overload dose yet.");
                return;
            }

            character.Interrupt(this);

            if (item.Id == PotionIds[0])
            {
                PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionIds[1]).Build(), ApplyEffect);
            }
            else if (item.Id == PotionIds[1])
            {
                PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionIds[2]).Build(), ApplyEffect);
            }
            else if (item.Id == PotionIds[2])
            {
                PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionIds[3]).Build(), ApplyEffect);
            }
            else if (item.Id == PotionIds[3])
            {
                PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionConstants.Vial).Build(), ApplyEffect);
            }
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            character.QueueTask(new OverloadBoostTask(character));
            character.AddState(new State(StateType.OverloadEffect, 500));
        }
    }
}