namespace Hagalaz.Game.Messages.Model
{
    public class RemoveClanChatMemberResponse
    {
        public const byte Opcode = 11;
        
        public long SessionId { get; set; }
        public bool Succeeded { get; set; }
    }
}