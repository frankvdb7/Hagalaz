using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData(typeof(StunState))]
    public class StunState : State, IStateScript
    {
        public override IStateScript Script => this;

        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public void OnStateAdded(IState state, ICreature creature)
        {
            creature.Movement.Lock(true);
            EventHappened? happ = null;
            happ = creature.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e =>
            {
                if (creature.HasState<IState>())
                {
                    if (creature is ICharacter character)
                    {
                        character.SendChatMessage(GameStrings.Stunned);
                    }

                    return true;
                }

                creature.UnregisterEventHandler<WalkAllowEvent>(happ!);
                return false;
            }));
        }

        /// <summary>
        ///     Gets called when the state is removed.
        ///     By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public void OnStateRemoved(IState state, ICreature creature) => creature.Movement.Unlock(false);

        public bool IsSerializable() => false; // Default implementation from StateScriptBase
    }
}