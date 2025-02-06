using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Equipment.Barrows;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    /// </summary>
    public class KarilsCrossbow : KarilEquipment
    {
        /// <summary>
        ///     Perform's standart attack to victim.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) => Crossbows.PerformCrossbowAttack(item, attacker, victim);

        /// <summary>
        ///     Happens when crossbow is equiped for this character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            base.OnEquiped(item, character);
            character.AddState(new State(StateType.CrossbowEquiped, int.MaxValue));
        }

        /// <summary>
        ///     Happens when crossbow is unequiped for this character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            base.OnUnequiped(item, character);
            character.RemoveState(StateType.CrossbowEquiped);
        }

        /// <summary>
        ///     Get's items suitable for this script.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int>  GetSuitableItems() => [4734, 4934, 4935, 4936, 4937];
    }
}