using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Auras
{
    [ItemScriptMetaData([23848, 23850, 23852, 23854])]
    public class HarmonyAura : AuraItemScript
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
                23850 => 2,
                23852 => 3,
                23854 => 4,
                _ => 1
            };
    }
}