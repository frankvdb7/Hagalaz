namespace Hagalaz.Game.Messages.Model
{
    public class RemoveFriendsChatMemberResponse
    {
        public const byte Opcode = 10;
        
        public uint CharacterId { get; set; }
        public bool Succeeded { get; set; }
    }
}