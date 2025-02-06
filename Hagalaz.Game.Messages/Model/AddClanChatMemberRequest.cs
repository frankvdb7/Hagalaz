namespace Hagalaz.Game.Messages.Model
{
    public class AddClanChatMemberRequest 
    {
        public const byte Opcode = 9;
        
        public int MasterId { get; set; }
        public bool GuestChannel { get; set; }
        public string ClanName { get; set; } = default!;
    }
}