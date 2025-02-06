namespace Hagalaz.Game.Messages.Model
{
    public class HandshakeResponse
    {
        public static readonly HandshakeResponse Success = new HandshakeResponse();
        
        public const byte Opcode = 1;

        public string? Error { get; }

        public HandshakeResponse(string? error = null) => Error = error;
    }
}