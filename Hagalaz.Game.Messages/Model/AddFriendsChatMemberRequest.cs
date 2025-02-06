namespace Hagalaz.Game.Messages.Model
{
    public class AddFriendsChatMemberRequest
    {
        public const byte Opcode = 8;
        
        public uint CharacterId { get; set; }
        public string OwnerDisplayName { get; set; } = default!;
    }
}