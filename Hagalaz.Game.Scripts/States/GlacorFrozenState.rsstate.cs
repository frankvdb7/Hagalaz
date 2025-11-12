using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData(typeof(GlacorFrozenState))]
    public class GlacorFrozenState : StateScriptBase
    {
        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateAdded(IState state, ICreature creature)
        {
            if (!(creature is ICharacter character))
            {
                return;
            }

            character.SendChatMessage("<col=00FFFF>You have been frozen. Try to escape as quickly as possible.");
            character.Movement.Lock(true);
            EventHappened? happ = null;
            happ = character.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e =>
            {
                if (creature.HasState(StateType.GlacorFrozen))
                {
                    if (creature is ICharacter)
                    {
                        //((Character)creature).SendMessage(GameMessages.MAGICAL_FORCE_MOVEMENT);
                    }

                    return true;
                }

                creature.UnregisterEventHandler<WalkAllowEvent>(happ!);
                return false;
            }));

            var mouseClicks = 0;
            EventHappened? mouseClick = null;
            mouseClick = character.RegisterEventHandler(new EventHappened<MouseClickEvent>(e =>
            {
                if (creature.HasState(StateType.GlacorFrozen))
                {
                    mouseClicks++;
                    if (mouseClicks < 5)
                    {
                        ((ICharacter)creature).SendChatMessage("<col=008B8B>The ice seems to weaken, keep trying to resist.");
                        return false;
                    }
                }

                ((ICharacter)creature).SendChatMessage("<col=00FFFF>You have resisted and are free from your icy bonds.");
                creature.RemoveState(StateType.GlacorFrozen);
                creature.UnregisterEventHandler<CharacterEvent>(mouseClick!);
                return false;
            }));
        }

        /// <summary>
        ///     Gets called when the state is removed.
        ///     By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateRemoved(IState state, ICreature creature) => creature.Movement.Unlock(false);
    }
}