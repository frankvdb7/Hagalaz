using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    /// </summary>
    public abstract class Potion : ItemScript
    {
        protected readonly int[] PotionIds;
        protected readonly IPotionSkillService PotionSkillService;
        protected readonly IHerbloreSkillService HerbloreSkillService;

        public Potion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService)
        {
            PotionSkillService = potionSkillService ?? throw new System.ArgumentNullException(nameof(potionSkillService));
            HerbloreSkillService = herbloreSkillService ?? throw new System.ArgumentNullException(nameof(herbloreSkillService));
            PotionIds = GetType().GetCustomAttribute<ItemScriptMetaDataAttribute>()?.ItemIds ?? [];
        }

        /// <summary>
        ///     Uses the item on an other item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character) =>
            PotionSkillService.CombinePotions(character, used, usedWith, PotionIds);

        /// <summary>
        ///     Happens when character clicks specific item in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.LeftClick)
            {
                if (new DrinkAllowEvent(character, item).Send())
                {
                    if (character.HasState(StateType.Stun) || character.HasState(StateType.Drinking))
                    {
                        return;
                    }

                    character.Interrupt(this);

                    var itemBuilder = character.ServiceProvider.GetRequiredService<IItemBuilder>();
                    if (item.Id == PotionIds[0])
                    {
                        PotionSkillService.DrinkPotion(character, item, itemBuilder.Create().WithId(PotionIds[1]).Build(), ApplyEffect);
                    }
                    else if (item.Id == PotionIds[1])
                    {
                        PotionSkillService.DrinkPotion(character, item, itemBuilder.Create().WithId(PotionIds[2]).Build(), ApplyEffect);
                    }
                    else if (item.Id == PotionIds[2])
                    {
                        PotionSkillService.DrinkPotion(character, item, itemBuilder.Create().WithId(PotionIds[3]).Build(), ApplyEffect);
                    }
                    else if (item.Id == PotionIds[3])
                    {
                        PotionSkillService.DrinkPotion(character, item, itemBuilder.Create().WithId(PotionConstants.Vial).Build(), ApplyEffect);
                    }

                    return;
                }
            }
            else if (clickType == ComponentClickType.Option7Click)
            {
                if (PotionSkillService.EmptyPotion(character, item))
                {
                    return;
                }
            }

            base.ItemClickedInInventory(clickType, item, character);
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected abstract void ApplyEffect(ICharacter character);
    }
}