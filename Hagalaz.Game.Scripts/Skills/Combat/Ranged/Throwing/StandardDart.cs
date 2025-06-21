using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([806, 807, 808, 809, 810, 811, 11230])]
    public class StandardDart : EquipmentScript
    {
        private readonly IThrowingLogicService _throwingService;

        public StandardDart(IThrowingLogicService throwingService) => _throwingService = throwingService;

        /// <summary>
        ///     Perform's standard ( Non special ) attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) =>
            _throwingService.PerformThrowingStandardAttack(item, attacker, victim);
    }
}