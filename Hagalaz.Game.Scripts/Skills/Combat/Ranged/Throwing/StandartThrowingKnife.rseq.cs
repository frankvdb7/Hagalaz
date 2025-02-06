using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    ///     Contains standart bow script.
    /// </summary>
    public class StandartThrowingKnife : EquipmentScript
    {
        /// <summary>
        ///     Performs the standart attack.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) => Throwing.PerformThrowingStandardAttack(item, attacker, victim);

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int>  GetSuitableItems() => [868, 867, 866, 865, 864, 863];
    }
}