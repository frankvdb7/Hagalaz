namespace Hagalaz.Game.Messages.Model
{
    public class RemoveContactResponse
    {
        public const byte Opcode = 22;
        
        public enum ResponseCode : byte
        {
            Success = 0,
            NotFound = 1,
            Fail = 2
        }
        
        public uint CharacterId { get; set; }
        public ResponseCode Response { get; set; }
        public ContactType? ContactType { get; set; }
        public uint? ContactMasterId { get; set; }
    }
}