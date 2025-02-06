using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Auras
{
    [ItemScriptMetaData([22905, 22907, 22909, 23874])]
    public class CorruptionAura : AuraItemScript
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
                22907 => 2,
                22909 => 3,
                23874 => 4,
                _ => 1
            };
    }
}