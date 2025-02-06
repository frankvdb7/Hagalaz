namespace Hagalaz.Game.Messages.Model
{
    public class RemoveFriendsChatMemberRequest
    {
        public const byte Opcode = 10;
        
        public uint CharacterId { get; init; }
    }
}