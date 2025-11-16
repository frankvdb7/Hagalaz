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
    [StateId("teleblocked")]
    public class TeleblockedState : StateBase
    {
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
                character.SendChatMessage(GameStrings.TeleBlocked);
            }

            EventHappened? happ = null;
            happ = creature.RegisterEventHandler(new EventHappened<TeleportAllowEvent>(e =>
            {
                if (creature.HasState<TeleblockedState>())
                {
                    if (creature is ICharacter)
                    {
                        ((ICharacter)creature).SendChatMessage(GameStrings.MagicalForceTeleport);
                    }

                    return true;
                }

                creature.UnregisterEventHandler<TeleportAllowEvent>(happ!);
                return false;
            }));
        }
    }
}