using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public class ClientSignInRequest : RaidoMessage
    {
        public int ClientRevision { get; init; }
        public int ClientRevisionPatch { get; init; }
        public string Login { get; init; } = default!;
        public string Password { get; init; } = default!;
        public uint[] IsaacSeed { get; init; } = default!;
        public int[] CacheCRCs { get; init; } = default!;
        public string ClientId { get; init; } = default!;
        public Language Language { get; init; }
        public DisplayMode DisplayMode { get; init; }
        public int ClientSizeX { get; init; }
        public int ClientSizeY { get; init; }
    }
}
