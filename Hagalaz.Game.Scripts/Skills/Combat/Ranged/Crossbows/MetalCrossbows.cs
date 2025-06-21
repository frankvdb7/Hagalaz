using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    ///     All metal crossbows equipment script
    /// </summary>
    [EquipmentScriptMetaData([9174, 9176, 9177, 9179, 13081, 9181, 9183, 9185, 13530, 837, 767, 11165, 11167, 18357])]
    public class MetalCrossbows : EquipmentScript
    {
        private readonly ICrossbowLogicService _crossbowLogicService;

        public MetalCrossbows(ICrossbowLogicService crossbowLogicService)
        {
            _crossbowLogicService = crossbowLogicService;
        }

        /// <summary>
        ///     Perform's standard attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim) =>
            _crossbowLogicService.PerformCrossbowAttack(item, attacker, victim);

        /// <summary>
        ///     Happens when crossbow is equiped for this character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.CrossbowEquiped, int.MaxValue));

        /// <summary>
        ///     Happens when crossbow is unequiped for this character.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.CrossbowEquiped);
    }
}