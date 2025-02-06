using System.Buffers;

namespace Hagalaz.Game.Messages.Model
{
    public class AddFriendsChatMessageRequest
    {
        public const byte Opcode = 17;
        
        public uint CharacterId { get; set; }
        public short MessageLength { get; set; }
        public ReadOnlySequence<byte> MessagePayload { get; set; }
    }
}