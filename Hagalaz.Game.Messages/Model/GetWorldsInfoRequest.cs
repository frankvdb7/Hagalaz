namespace Hagalaz.Game.Messages.Model
{
    public class GetWorldsInfoRequest
    {
        public const byte Opcode = 5;
        
        public uint CharacterId { get; set; }
        public bool ShouldSendInformation { get; set; }
    }
}