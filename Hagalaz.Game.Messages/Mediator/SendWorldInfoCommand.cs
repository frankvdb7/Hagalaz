using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Messages.Mediator
{
    public record SendWorldInfoCommand(IGameSession Session)
    {
        public int Checksum { get; init; }
    }
}
