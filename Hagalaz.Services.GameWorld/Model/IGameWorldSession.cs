using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Model;

public interface IGameWorldSession : IGameSession
{
    string SessionClaimId { get; }
}
