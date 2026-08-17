using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the narrow state activation and identity contract used by character persistence.
    /// </summary>
    public interface IStateService
    {
        /// <summary>
        /// Creates a state for a registered state ID.
        /// </summary>
        bool TryCreateState(string stateId, [NotNullWhen(true)] out IState? state);

        /// <summary>
        /// Resolves the registered stable ID for a state instance.
        /// </summary>
        bool TryGetStateId(IState state, [NotNullWhen(true)] out string? stateId);
    }
}
