namespace Hagalaz.Game.Messages.Model
{
    public class AddContactRequest
    {
        public const byte Opcode = 21;
        
        public uint CharacterId { get; set; }
        public string ContactDisplayName { get; set; } = default!;
        public ContactType ContactType { get; set; }
    }
}