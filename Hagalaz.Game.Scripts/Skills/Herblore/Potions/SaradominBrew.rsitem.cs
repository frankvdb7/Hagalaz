using System;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains saradomin brew script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [6685, 6687, 6689, 6691])]
    public class SaradominBrew : Potion
    {
        private readonly IItemBuilder _itemBuilder;

        public SaradominBrew(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IItemBuilder itemBuilder) : base(
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
            if (clickType == ComponentClickType.LeftClick)
            {
                if (new DrinkAllowEvent(character, item).Send())
                {
                    if (character.HasState(StateType.Stun) || character.HasState(StateType.Drinking))
                    {
                        return;
                    }

                    character.Interrupt(this);

                    if (item.Id == PotionIds[0])
                    {
                        PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionIds[1]).Build(), ApplyEffect, 1);
                    }
                    else if (item.Id == PotionIds[1])
                    {
                        PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionIds[2]).Build(), ApplyEffect, 1);
                    }
                    else if (item.Id == PotionIds[2])
                    {
                        PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionIds[3]).Build(), ApplyEffect, 1);
                    }
                    else if (item.Id == PotionIds[3])
                    {
                        PotionSkillService.DrinkPotion(character, item, _itemBuilder.Create().WithId(PotionConstants.Vial).Build(), ApplyEffect, 1);
                    }
                }
            }
            else if (clickType == ComponentClickType.Option7Click)
            {
                var slot = character.Inventory.GetInstanceSlot(item);
                if (slot == -1)
                {
                    return;
                }

                character.Inventory.Replace(slot, _itemBuilder.Create().WithId(PotionConstants.Vial).Build());
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            byte[] toDrain = [StatisticsConstants.Strength, StatisticsConstants.Attack, StatisticsConstants.Magic, StatisticsConstants.Ranged];
            for (byte i = 0; i < toDrain.Length; i++)
            {
                character.Statistics.DamageSkill(toDrain[i], (int)Math.Floor(0.10 * character.Statistics.LevelForExperience(i)));
            }

            character.Statistics.HealSkill(StatisticsConstants.Defence,
                (int)Math.Floor(1.25 * character.Statistics.LevelForExperience(StatisticsConstants.Defence)),
                (int)Math.Floor(0.25 * character.Statistics.LevelForExperience(StatisticsConstants.Defence)));
            character.Statistics.HealLifePoints((int)Math.Floor(0.3 * (character.Statistics.LevelForExperience(StatisticsConstants.Constitution) * 10.0)) + 20,
                (int)Math.Floor(1.3 * character.Statistics.GetMaximumLifePoints()));
        }
    }
}