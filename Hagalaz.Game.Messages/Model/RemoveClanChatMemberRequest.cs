namespace Hagalaz.Game.Messages.Model
{
    public class RemoveClanChatMemberRequest
    {
        public const byte Opcode = 11;
        
        public long SessionId { get; set; }
    }
}