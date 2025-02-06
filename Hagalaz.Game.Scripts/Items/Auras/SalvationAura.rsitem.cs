using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Auras
{
    [ItemScriptMetaData([22899, 22901, 22903, 23876])]
    public class SalvationAura : AuraItemScript
    {
        /// <summary>
        ///     Gets the tier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public override int GetTier(ICharacter character, IItem aura) =>
            aura.Id switch
            {
                22901 => 2,
                22903 => 3,
                23876 => 4,
                _ => 1
            };
    }
}