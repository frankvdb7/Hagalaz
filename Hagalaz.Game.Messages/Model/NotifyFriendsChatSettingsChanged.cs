namespace Hagalaz.Game.Messages.Model
{
    public class NotifyFriendsChatSettingsChanged
    {
        public record FriendsChatSettingsDto
        {
            
        }
        
        public required FriendsChatSettingsDto Settings { get; init; }
    }
}