using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    ///     Contains standard throwing knife script
    /// </summary>
    [EquipmentScriptMetaData([868, 867, 866, 865, 864, 863])]
    public class StandardThrowingKnife : EquipmentScript
    {
        private readonly IThrowingLogicService _throwingLogicService;

        public StandardThrowingKnife(IThrowingLogicService throwingLogicService) => _throwingLogicService = throwingLogicService;

        /// <summary>
        ///     Performs the standard attack.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) =>
            _throwingLogicService.PerformThrowingStandardAttack(item, attacker, victim);
    }
}