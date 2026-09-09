namespace Hagalaz.Game.Messages.Mediator
{
    public record WorldSignOutCommand(uint MasterId, long SessionGeneration, string ConnectionId);
}
