using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Messages.Mediator
{
    public record LobbySignInCommand(uint MasterId, IGameSession GameSession);
}
