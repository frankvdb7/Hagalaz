using System;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public class LobbySignInResponse : ClientSignInResponse
    {
        public LobbySignInResponse() => Succeeded = true;

        public string DisplayName { get; init; } = default!;
        public int ClientPermissions { get; init; }
        public DateTimeOffset? LastLogin { get; init; }
        public string? LastIpAddress { get; init; }
        public int UnreadMessagesCount { get; init; }
        public int WorldId { get; init; }
        public string WorldAddress { get; init; } = default!;
    }
}