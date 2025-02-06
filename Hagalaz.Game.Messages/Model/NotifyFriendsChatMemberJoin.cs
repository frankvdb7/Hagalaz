namespace Hagalaz.Game.Messages.Model
{
    public class NotifyFriendsChatMemberJoin
    {
        public const byte Opcode = 14;
        
        public string ChatName { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? PreviousDisplayName { get; set; }
        public int WorldId { get; set; }
        public object Rank { get; set; }
    }
}