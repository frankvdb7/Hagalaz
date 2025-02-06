namespace Hagalaz.Game.Messages.Model
{
    public class DoFriendsChatMemberKickRequest
    {
        public const byte Opcode = 24;
        
        public uint CharacterId { get; init; }
        public string MemberDisplayName { get; init; } = default!;
    }
}