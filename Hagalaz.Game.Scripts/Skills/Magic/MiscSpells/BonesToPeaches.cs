using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    /// <summary>
    ///     Contains bones to peaches script.
    /// </summary>
    public class BonesToPeaches : IBonesToPeaches
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly ICharacter _caster;

        /// <summary>
        /// </summary>
        private static readonly RuneType[] _runes = [RuneType.Nature, RuneType.Water, RuneType.Earth];

        /// <summary>
        /// </summary>
        private static readonly int[] _runeAmounts = [2, 4, 4];

        public BonesToPeaches(IItemBuilder itemBuilder, ICharacterContextAccessor characterContextAccessor)
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
            return BonesConversion.TryConvert(_caster, _itemBuilder, 6883, 35.5);
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        private static bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(60) && caster.Magic.CheckRunes(_runes, _runeAmounts);

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        private static void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runes, _runeAmounts);
    }
}
