using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.States;
using Hagalaz.Game.Scripts.Tasks;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData(typeof(OverloadEffectState))]
    public class OverloadEffectState : StateScriptBase
    {
        /// <summary>
        ///     Determines whether this instance is serializable.
        ///     By default, the state is not serializable.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerializable() => true;

        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateAdded(IState state, ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.QueueTask(new OverloadTask(character, state.TicksLeft));
            }
        }

        /// <summary>
        ///     Gets called when the state is removed.
        ///     By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateRemoved(IState state, ICreature creature)
        {
            if (creature is not ICharacter character)
            {
                return;
            }

            character.Statistics.NormaliseSkills([StatisticsConstants.Attack, StatisticsConstants.Strength, StatisticsConstants.Defence, StatisticsConstants.Ranged, StatisticsConstants.Magic
            ]);
            character.Statistics.HealLifePoints(500);
            character.SendChatMessage("The effect of the overload potion has worn off.");
        }
    }
}