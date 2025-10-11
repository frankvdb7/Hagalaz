using System;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the contract for a temporary state or effect that can be applied to a character, such as poison, stun, or a temporary boost.
    /// States are processed on each game tick and can expire after a certain duration.
    /// </summary>
    public interface IState : ITickItem, IDisposable
    {
        /// <summary>
        /// Gets the delay in game ticks before the state is automatically removed.
        /// </summary>
        int RemoveDelay { get; }

        /// <summary>
        /// Gets the type of the state, which categorizes its general effect (e.g., Skull, Vengeance).
        /// </summary>
        StateType StateType { get; }

        /// <summary>
        /// Gets the script that defines the behavior and logic of this state.
        /// </summary>
        IStateScript Script { get; }

        /// <summary>
        /// Gets a value indicating whether this state has been marked for removal.
        /// </summary>
        bool Removed { get; }
    }
}
