using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    public interface ICrossbowLogicService
    {
        /// <summary>
        ///     Perform's crossbow attack.
        /// </summary>
        void PerformCrossbowAttack(IItem item, ICharacter attacker, ICreature victim);
    }
}