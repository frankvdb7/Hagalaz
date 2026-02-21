namespace Hagalaz.Game.Messages.Model
{
    public class SetFriendsChatSettingsRequest
    {
        public const byte Opcode = 24;

        public record FriendsChatSettingsDto
        {
            
        }
        
        public required FriendsChatSettingsDto Settings { get; init; }
    }
}