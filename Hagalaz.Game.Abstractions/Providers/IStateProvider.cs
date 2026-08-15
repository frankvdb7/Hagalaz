using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Providers
{
    public interface IStateProvider
    {
        bool TryCreateState(string id, [NotNullWhen(true)] out IState? state);

        bool TryGetStateId(IState state, [NotNullWhen(true)] out string? id);
    }
}
