using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Equipment.Barrows;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4734, 4934, 4935, 4936, 4937])]
    public class KarilsCrossbow : KarilEquipment
    {
        private readonly ICrossbowLogicService _crossbowLogicService;

        public KarilsCrossbow(ICrossbowLogicService crossbowLogicService)
        {
            _crossbowLogicService = crossbowLogicService;
        }

        /// <summary>
        ///     Perform's standard attack to victim.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) =>
            _crossbowLogicService.PerformCrossbowAttack(item, attacker, victim);

        /// <summary>
        ///     Happens when crossbow is equiped for this character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            base.OnEquiped(item, character);
            character.AddState(new CrossbowEquipedState());
        }

        /// <summary>
        ///     Happens when crossbow is unequiped for this character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            base.OnUnequiped(item, character);
            character.RemoveState<CrossbowEquipedState>();
        }
    }
}