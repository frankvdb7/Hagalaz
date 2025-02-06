namespace Hagalaz.Game.Messages.Model
{
    public class NotifyClanSettingsChanged
    {
        public record ClanSettingsDto
        {
            
        }
        
        public ClanSettingsDto Settings { get; init; }
    }
}