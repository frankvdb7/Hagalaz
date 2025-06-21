using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    public interface IThrowingLogicService
    {
        /// <summary>
        ///     Perform's standard throwing attack.
        /// </summary>
        void PerformThrowingStandardAttack(IItem item, ICharacter attacker, ICreature victim);
    }
}