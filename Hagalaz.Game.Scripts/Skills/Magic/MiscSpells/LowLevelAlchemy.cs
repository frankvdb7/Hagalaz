using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Resources;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    /// <summary>
    ///     Contains low level alchemy script.
    /// </summary>
    public static class LowLevelAlchemy
    {
        /// <summary>
        /// </summary>
        private static readonly RuneType[] Runes = [RuneType.Fire, RuneType.Nature];

        /// <summary>
        /// </summary>
        private static readonly int[] Runeamounts = [3, 1];

        /// <summary>
        ///     Casts the spell.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static bool Cast(ICharacter caster, IItem item)
        {
            if (caster.HasState(StateType.Alching))
            {
                return false;
            }

            if (!CheckRequirements(caster))
            {
                return false;
            }

            var slot = caster.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return false;
            }

            var coins = new Item(995, item.ItemDefinition.LowAlchemyValue);
            if (!caster.Inventory.HasSpaceFor(coins) && !caster.MoneyPouch.HasSpaceFor(coins))
            {
                caster.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            RemoveRequirements(caster);
            var removed = caster.Inventory.Remove(new Item(item.Id, 1), slot);
            if (removed > 0)
            {
                if (caster.Inventory.Add(coins))
                {
                    caster.QueueAnimation(Animation.Create(712));
                    caster.QueueGraphic(Graphic.Create(112));
                    caster.Statistics.AddExperience(StatisticsConstants.Magic, 31);
                    caster.Configurations.SendGlobalCs2Int(168, 7); // set active tab.
                    caster.AddState(new State(StateType.Alching, 2));
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public static bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(21) && caster.Magic.CheckRunes(Runes, Runeamounts);

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        public static void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(Runes, Runeamounts);
    }
}