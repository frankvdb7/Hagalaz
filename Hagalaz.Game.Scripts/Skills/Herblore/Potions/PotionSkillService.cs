using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    public class PotionSkillService : IPotionSkillService
    {
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     Delegate for drinking the potion.
        /// </summary>
        public delegate void OnFinish(ICharacter character);

        public PotionSkillService(IItemBuilder itemBuilder) => _itemBuilder = itemBuilder;

        /// <summary>
        ///     Drinks the potion.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="current">The current.</param>
        /// <param name="next">The next.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="drinkingTicks">The drinking ticks.</param>
        /// <param name="message">The message.</param>
        public void DrinkPotion(ICharacter character, IItem current, IItem next, OnFinish finish, int drinkingTicks = 2, string? message = null)
        {
            var slot = character.Inventory.GetInstanceSlot(current);
            if (slot == -1)
            {
                return;
            }

            if (current.Count <= 0)
            {
                return;
            }

            character.Inventory.Replace(slot, next);

            character.AddState(new State(StateType.Drinking, drinkingTicks));
            character.QueueAnimation(Animation.Create(829));

            var nameArray = current.Name.Split('(');
            if (!int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out var doses))
            {
                return;
            }
            if (doses > 0)
            {
                doses -= 1;
            }

            if (message == null)
            {
                character.SendChatMessage("You drink some of your " + nameArray[0].Remove(nameArray[0].Length - 1, 1).ToLower() + ".");
            }
            else
            {
                character.SendChatMessage(message);
            }

            if (doses == 0)
            {
                character.SendChatMessage("You have finished your potion.");
            }
            else if (doses == 1)
            {
                character.SendChatMessage("You have 1 dose of potion left.");
            }
            else
            {
                character.SendChatMessage("You have " + doses + " doses of potion left.");
            }

            finish.Invoke(character);
        }

        /// <summary>
        ///     Combines the potions.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="potionIds">The potion ids.</param>
        /// <returns></returns>
        public bool CombinePotions(ICharacter character, IItem used, IItem usedWith, int[] potionIds)
        {
            var usedSlot = character.Inventory.GetInstanceSlot(used);
            if (usedSlot == -1)
            {
                return false;
            }

            var usedWithSlot = character.Inventory.GetInstanceSlot(usedWith);
            if (usedWithSlot == -1)
            {
                return false;
            }

            IItem? newUsed = null;
            IItem? newUsedWith = null;

            if (usedWith.Id == potionIds[0])
            {
                // Potion is already full.
                return true;
            }

            if (used.Id == potionIds[0])
            {
                newUsed = usedWith.Clone();
                newUsedWith = used.Clone();
            }
            else if (used.Id == potionIds[1] && usedWith.Id == potionIds[1])
            {
                newUsed = _itemBuilder.Create().WithId(potionIds[2]).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[0]).Build();
            }
            else if (used.Id == potionIds[2] && usedWith.Id == potionIds[2])
            {
                newUsed = _itemBuilder.Create().WithId(PotionConstants.Vial).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[0]).Build();

            }
            else if (used.Id == potionIds[3] && usedWith.Id == potionIds[3])
            {
                newUsed = _itemBuilder.Create().WithId(PotionConstants.Vial).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[2]).Build();
            }
            else if (used.Id == potionIds[1] && usedWith.Id == potionIds[2])
            {
                newUsed = _itemBuilder.Create().WithId(potionIds[3]).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[0]).Build();
            }
            else if (used.Id == potionIds[1] && usedWith.Id == potionIds[3])
            {
                newUsed = _itemBuilder.Create().WithId(PotionConstants.Vial).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[0]).Build();
            }
            else if (used.Id == potionIds[2] && usedWith.Id == potionIds[1])
            {
                newUsed = _itemBuilder.Create().WithId(potionIds[3]).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[0]).Build();
            }
            else if (used.Id == potionIds[2] && usedWith.Id == potionIds[3])
            {
                newUsed = _itemBuilder.Create().WithId(PotionConstants.Vial).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[1]).Build();
            }
            else if (used.Id == potionIds[3] && usedWith.Id == potionIds[2])
            {
                newUsed = _itemBuilder.Create().WithId(PotionConstants.Vial).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[1]).Build();
            }
            else if (used.Id == potionIds[3] && usedWith.Id == potionIds[1])
            {
                newUsed = _itemBuilder.Create().WithId(PotionConstants.Vial).Build();
                newUsedWith = _itemBuilder.Create().WithId(potionIds[0]).Build();
            }


            if (newUsed != null && newUsedWith != null)
            {
                character.Inventory.Replace(usedSlot, newUsed);
                character.Inventory.Replace(usedWithSlot, newUsedWith);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Empties the potion.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="potion">The potion.</param>
        /// <returns></returns>
        public bool EmptyPotion(ICharacter character, IItem potion)
        {
            var slot = character.Inventory.GetInstanceSlot(potion);
            if (slot == -1)
            {
                return false;
            }

            character.Inventory.Replace(slot, _itemBuilder.Create().WithId(PotionConstants.Vial).Build());
            return true;
        }
    }
}
