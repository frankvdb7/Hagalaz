namespace Hagalaz.Game.Messages.Model
{
    public class SetContactSettingsRequest
    {
        public const byte Opcode = 23;
        
        public uint CharacterId { get; init; }
        public ContactAvailability Availability { get; init; }
    }
}