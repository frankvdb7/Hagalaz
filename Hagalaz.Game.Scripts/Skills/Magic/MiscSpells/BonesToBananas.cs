using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    /// <summary>
    ///     Contains bones to bananas script.
    /// </summary>
    public class BonesToBananas : IBonesToBananas
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly ICharacter _caster;

        /// <summary>
        /// </summary>
        private static readonly RuneType[] _runes = [RuneType.Earth, RuneType.Water, RuneType.Nature];

        /// <summary>
        /// </summary>
        private static readonly int[] _runeAmounts = [2, 2, 1];

        public BonesToBananas(IItemBuilder itemBuilder, ICharacterContextAccessor characterContextAccessor)
        {
            _itemBuilder = itemBuilder;
            _caster = characterContextAccessor.Context.Character;
        }

        /// <summary>
        ///     Casts the spell.
        /// </summary>
        /// <returns></returns>
        public bool Cast()
        {
            if (!CheckRequirements(_caster))
            {
                return false;
            }

            RemoveRequirements(_caster);
            return BonesConversion.TryConvert(_caster, _itemBuilder, 1963, 25);
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        private static bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(15) && caster.Magic.CheckRunes(_runes, _runeAmounts);

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        private static void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runes, _runeAmounts);
    }
}
