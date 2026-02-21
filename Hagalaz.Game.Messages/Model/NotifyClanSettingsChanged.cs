namespace Hagalaz.Game.Messages.Model
{
    public class NotifyClanSettingsChanged
    {
        public record ClanSettingsDto
        {
            
        }
        
        public required ClanSettingsDto Settings { get; init; }
    }
}