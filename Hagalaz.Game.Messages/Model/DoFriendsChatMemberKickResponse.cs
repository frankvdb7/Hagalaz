namespace Hagalaz.Game.Messages.Model
{
    public class DoFriendsChatMemberKickResponse
    {
        public const byte Opcode = 26;
        
        public enum ResponseCode : byte
        {
            Success = 0,
            Failed = 1,
            NotFound = 2,
            LowRank = 3
        }
        
        public uint CharacterId { get; init; }
        public ResponseCode Response { get; init; }
    }
}