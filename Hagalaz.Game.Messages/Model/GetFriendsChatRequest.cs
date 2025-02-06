namespace Hagalaz.Game.Messages.Model
{
    public class GetFriendsChatRequest
    {
        public const byte Opcode = 16;
        
        public uint CharacterId { get; set; }
    }
}