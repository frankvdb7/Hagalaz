namespace Hagalaz.Game.Messages.Model
{
    public class RemoveContactRequest
    {
        public const byte Opcode = 22;

        public uint CharacterId { get; set; }
        public string ContactDisplayName { get; set; } = default!;
        public ContactType ContactType { get; set; }
    }
}