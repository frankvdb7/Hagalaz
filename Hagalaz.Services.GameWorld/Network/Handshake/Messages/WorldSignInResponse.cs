namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public class WorldSignInResponse : ClientSignInResponse
    {
        public WorldSignInResponse() => Succeeded = true;

        public string DisplayName { get; init; } = default!;
        public int ClientPermissions { get; init; }
        public bool IsQuickChatOnly { get; init; }
        public int CharacterWorldIndex { get; init; }
        public bool IsMembersOnly { get; init; }
    }
}