namespace Hagalaz.Game.Messages.Model
{
    public class RemoveCharacterRequest
    {
        public const byte Opcode = 4;
        
        public uint CharacterId { get; set; }
    }
}