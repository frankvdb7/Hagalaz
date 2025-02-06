using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetClientWindowMessage : RaidoMessage
    {
        public required int SizeX { get; init; }
        public required int SizeY { get; init; }
        public required DisplayMode Mode { get; init; }
    }
}
