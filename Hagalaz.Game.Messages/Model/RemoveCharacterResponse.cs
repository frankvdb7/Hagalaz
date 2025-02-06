namespace Hagalaz.Game.Messages.Model
{
    public class RemoveCharacterResponse
    {
        public const byte Opcode = 4;
        
        public uint CharacterId { get; set; }
        public bool Succeeded { get; set; }
    }
}