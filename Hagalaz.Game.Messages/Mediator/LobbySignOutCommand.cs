namespace Hagalaz.Game.Messages.Mediator
{
    public record LobbySignOutCommand(uint MasterId, long SessionGeneration, string ConnectionId);
}
