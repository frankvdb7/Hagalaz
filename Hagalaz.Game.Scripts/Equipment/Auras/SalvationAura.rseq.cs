using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Auras
{
    [EquipmentScriptMetaData([22899, 22901, 22903, 23876])]
    public class SalvationAura : AuraEquipmentScript
    {
        /// <summary>
        ///     Gets the secondary model identifier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public override int GetSecondaryModelID(ICharacter character, IItem aura)
        {
            if (aura.Id == 22901)
            {
                return 16524;
            }

            if (aura.Id == 22903)
            {
                return 16450;
            }

            if (aura.Id == 23876)
            {
                return 68611;
            }

            return 16449;
        }
    }
}