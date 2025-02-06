using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class PlayMusicEffectMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int Volume { get; init; }
    }
}
