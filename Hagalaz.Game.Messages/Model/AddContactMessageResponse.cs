using System.Buffers;

namespace Hagalaz.Game.Messages.Model
{
    public class AddContactMessageResponse
    {
        public const byte Opcode = 19;
        
        public enum ResponseCode : byte
        {
            Success = 0,
            NotFound = 1,
            Ignored = 2
        }
        
        public uint CharacterId { get; init; }
        public int? MessageLength { get; init; }
        public ReadOnlySequence<byte>? MessagePayload { get; init; }
        public string? ContactDisplayName { get; init; }
        public ResponseCode Response { get; init; }
    }
}