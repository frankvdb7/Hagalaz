namespace Hagalaz.Game.Messages.Model
{
    public class HandshakeRequest
    {
        public const byte Opcode = 1;
        
        public int WorldId { get; }

        public HandshakeRequest(int worldId) => WorldId = worldId;
    }
}