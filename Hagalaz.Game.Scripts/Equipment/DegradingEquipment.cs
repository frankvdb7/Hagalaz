using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment
{
    /// <summary>
    /// </summary>
    public abstract class DegradingEquipment : EquipmentScript
    {
        /// <summary>
        ///     Get's called after a character performed an attack.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="target">The target.</param>
        public override void OnAttackPerformed(IItem item, ICharacter attacker, ICreature target)
        {
            var remainingTicks = item.ExtraData[0];
            if (remainingTicks == -1)
            {
                remainingTicks = GetDegrationTicks(item);
            }

            item.ExtraData[0] = remainingTicks - attacker.Combat.DelayTick;
            CheckDegration(item, attacker);
        }

        /// <summary>
        ///     Get's called after a character has died.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnDeath(IItem item, ICharacter character)
        {
            var remainingTicks = item.ExtraData[0];
            if (remainingTicks == -1)
            {
                remainingTicks = GetDegrationTicks(item);
            }

            item.ExtraData[0] = (int)(remainingTicks * GetDegrationOnDeathFactor(item));
            CheckDegration(item, character);
        }

        private void CheckDegration(IItem item, ICharacter character)
        {
            var remainingTicks = item.ExtraData[0];
            if (remainingTicks > 0)
            {
                return;
            }

            var itemBuilder = character.ServiceProvider.GetRequiredService<IItemBuilder>();
            var degradedItemID = GetDegradedItemID(item);
            if (degradedItemID != -1 && degradedItemID != item.Id)
            {
                var name = item.Name;
                var slot = character.Equipment.GetInstanceSlot(item);
                if (slot == EquipmentSlot.NoSlot)
                {
                    return;
                }

                character.Equipment.Replace(slot, itemBuilder.Create().WithId(degradedItemID).WithCount(item.Count).Build());
                character.SendChatMessage("Your " + name + " degraded.");

                item.ExtraData[0] = GetDegrationTicks(item);
            }
            else if (degradedItemID == -1)
            {
                var name = item.Name;
                var slot = character.Equipment.GetInstanceSlot(item);
                if (slot == EquipmentSlot.NoSlot)
                {
                    return;
                }

                character.Equipment.Remove(item, slot);
                character.SendChatMessage("Your " + name + " crumbles into dust.");
            }
        }

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public abstract int GetDegrationTicks(IItem item);

        /// <summary>
        ///     Gets the degration on death factor.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public abstract double GetDegrationOnDeathFactor(IItem item);

        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public abstract int GetDegradedItemID(IItem item);
    }
}