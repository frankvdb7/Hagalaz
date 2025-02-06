using System.Buffers;

namespace Hagalaz.Game.Messages.Model
{
    public class AddContactMessageRequest
    {
        public const byte Opcode = 19;
        
        public uint CharacterId { get; set; }
        public string ContactDisplayName { get; set; } = default!;
        public short MessageLength { get; set; }
        public ReadOnlySequence<byte> MessagePayload { get; set; }
    }
}