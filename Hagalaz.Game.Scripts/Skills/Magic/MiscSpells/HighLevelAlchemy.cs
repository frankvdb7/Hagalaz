using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    /// <summary>
    ///     Contains high level alchemy script.
    /// </summary>
    public class HighLevelAlchemy : IHighLevelAlchemy
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly ICharacter _caster;

        /// <summary>
        /// </summary>
        private static readonly RuneType[] _runes = [RuneType.Fire, RuneType.Nature];

        /// <summary>
        /// </summary>
        private static readonly int[] _runeAmounts = [5, 1];

        public HighLevelAlchemy(IItemBuilder itemBuilder, ICharacterContextAccessor characterContextAccessor)
        {
            _itemBuilder = itemBuilder;
            _caster = characterContextAccessor.Context.Character;
        }

        /// <summary>
        ///     Casts the spell.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool Cast(IItem item)
        {
            return AlchemyOperations.Cast(_caster, item, _itemBuilder, item.ItemDefinition.HighAlchemyValue, 713, 113, 65,
                () => CheckRequirements(_caster), () => RemoveRequirements(_caster));
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        private static bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(55) && caster.Magic.CheckRunes(_runes, _runeAmounts);

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        private static void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runes, _runeAmounts);
    }
}
