using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Auras
{
    [EquipmentScriptMetaData([22905, 22907, 22909, 23874])]
    public class CorruptionAura : AuraEquipmentScript
    {
        /// <summary>
        ///     Gets the model identifier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public override int GetSecondaryModelID(ICharacter character, IItem aura)
        {
            if (aura.Id == 22907)
            {
                return 16464;
            }

            if (aura.Id == 22909)
            {
                return 16429;
            }

            if (aura.Id == 23874)
            {
                return 68615;
            }

            return 16449;
        }
    }
}