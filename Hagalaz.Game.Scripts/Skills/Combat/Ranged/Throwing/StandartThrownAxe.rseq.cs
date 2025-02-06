using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    /// </summary>
    public class StandartThrownAxe : EquipmentScript
    {
        /// <summary>
        ///     Perform's standart ( Non special ) attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) => Throwing.PerformThrowingStandardAttack(item, attacker, victim);

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int>  GetSuitableItems() => [800, 801, 802, 803, 804, 805];
    }
}