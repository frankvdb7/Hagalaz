using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    public class GlacorFrozenState : ScriptedState
    {
        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnApply(ICreature creature)
        {
            if (!(creature is ICharacter character))
            {
                return;
            }

            character.SendChatMessage("<col=00FFFF>You have been frozen. Try to escape as quickly as possible.");
            character.Movement.Lock(true);

            var mouseClicks = 0;
            EventHappened? mouseClick = null;

            mouseClick = character.RegisterEventHandler<GameObjectClickPerformEvent>(e =>
            {
                if (creature.HasState<GlacorFrozenState>())
                {
                    mouseClicks++;
                    if (mouseClicks < 5)
                    {
                        ((ICharacter)creature).SendChatMessage("<col=008B8B>The ice seems to weaken, keep trying to resist.");
                        return false;
                    }
                }

                ((ICharacter)creature).SendChatMessage("<col=00FFFF>You have resisted and are free from your icy bonds.");
                creature.RemoveState<GlacorFrozenState>();
                creature.UnregisterEventHandler<GameObjectClickPerformEvent>(mouseClick!);
                return false;
            });
        }

        /// <summary>
        ///     Gets called when the state is removed.
        ///     By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnRemove(ICreature creature) => creature.Movement.Unlock(false);
    }
}