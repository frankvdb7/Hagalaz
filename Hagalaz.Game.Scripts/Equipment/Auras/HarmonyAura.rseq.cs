using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Auras
{
    [EquipmentScriptMetaData([23848, 23850, 23852, 23854])]
    public class HarmonyAura : AuraEquipmentScript
    {
        /// <summary>
        ///     Gets the model identifier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public override int GetSecondaryModelID(ICharacter character, IItem aura)
        {
            if (aura.Id == 23850)
            {
                return 68610;
            }

            if (aura.Id == 23852)
            {
                return 68607;
            }

            if (aura.Id == 23854)
            {
                return 68613;
            }

            return 68605;
        }
    }
}