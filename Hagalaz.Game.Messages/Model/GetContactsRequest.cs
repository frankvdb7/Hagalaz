namespace Hagalaz.Game.Messages.Model
{
    public class GetContactsRequest
    {
        public const byte Opcode = 6;

        public uint CharacterId { get; set; }
    }
}