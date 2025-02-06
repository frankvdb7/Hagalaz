namespace Hagalaz.Game.Messages.Model
{
    public class SetFriendsChatSettingsRequest
    {
        public const byte Opcode = 24;

        public record FriendsChatSettingsDto
        {
            
        }
        
        public FriendsChatSettingsDto Settings { get; init; }
    }
}