using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([825, 826, 827, 828, 829, 830])]
    public class StandardJavelin : EquipmentScript
    {
        private readonly IThrowingLogicService _throwingLogicService;
        public StandardJavelin(IThrowingLogicService throwingLogicService) => _throwingLogicService = throwingLogicService;

        /// <summary>
        ///     Perform's standard ( Non special ) attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) =>
            _throwingLogicService.PerformThrowingStandardAttack(item, attacker, victim);
    }
}