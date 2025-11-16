using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines a contract for a service that provides information about states.
    /// </summary>
    public interface IStateService
    {
        /// <summary>
        /// Gets the state for a given state ID.
        /// </summary>
        /// <param name="stateId">The ID of the state.</param>
        /// <returns>The state instance if found; otherwise, <c>null</c>.</returns>
        IState? GetState(string stateId);
    }
}