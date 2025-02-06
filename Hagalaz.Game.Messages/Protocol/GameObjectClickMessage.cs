using Hagalaz.Game.Abstractions.Model.GameObjects;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class GameObjectClickMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int AbsX { get; init; }
        public required int AbsY { get; init; }
        public required GameObjectClickType ClickType { get; init; }
        public required bool ForceRun { get; init; }
    }
}
