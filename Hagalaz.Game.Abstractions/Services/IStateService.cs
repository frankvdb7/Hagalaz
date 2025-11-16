using System.Threading.Tasks;
using FluentResults;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines a contract for a service that provides information about states.
    /// </summary>
    public interface IStateService
    {
        /// <summary>
        /// Gets the state type for a given state ID.
        /// </summary>
        /// <param name="stateId">The ID of the state.</param>
        /// <returns>The type of the state.</returns>
        IState? GetState(string stateId);
    }
}
