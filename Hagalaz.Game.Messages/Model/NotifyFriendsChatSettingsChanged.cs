namespace Hagalaz.Game.Messages.Model
{
    public class NotifyFriendsChatSettingsChanged
    {
        public record FriendsChatSettingsDto
        {
            
        }
        
        public FriendsChatSettingsDto Settings { get; init; }
    }
}