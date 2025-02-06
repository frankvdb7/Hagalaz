using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    /// <summary>
    ///     Contains bones to peaches script.
    /// </summary>
    public static class BonesToPeaches
    {
        /// <summary>
        /// </summary>
        private static readonly RuneType[] Runes = [RuneType.Nature, RuneType.Water, RuneType.Earth];

        /// <summary>
        /// </summary>
        private static readonly int[] Runeamounts = [2, 4, 4];

        /// <summary>
        ///     Casts the spell.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public static bool Cast(ICharacter caster)
        {
            if (!CheckRequirements(caster))
            {
                return false;
            }

            RemoveRequirements(caster);
            var removed = caster.Inventory.Remove(new Item(526, caster.Inventory.Capacity));
            removed += caster.Inventory.Remove(new Item(532, caster.Inventory.Capacity));
            if (removed > 0)
            {
                caster.QueueAnimation(Animation.Create(722));
                caster.QueueGraphic(Graphic.Create(141, 0, 100));
                caster.Inventory.Add(new Item(6883, removed));
                caster.Statistics.AddExperience(StatisticsConstants.Magic, 35.5 * removed);
            }
            else
            {
                caster.SendChatMessage("You don't have any bones to cast this spell on.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public static bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(60) && caster.Magic.CheckRunes(Runes, Runeamounts);

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        public static void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(Runes, Runeamounts);
    }
}