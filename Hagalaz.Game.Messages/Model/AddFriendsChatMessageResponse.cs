namespace Hagalaz.Game.Messages.Model
{
    public class AddFriendsChatMessageResponse
    {
        public enum ResponseCode : byte
        {
            Success,
            NotAllowed,
            NotFound
        }
        
        public const byte Opcode = 17;
        
        public uint CharacterId { get; set; }
        public ResponseCode Response { get; set; }
    }
}