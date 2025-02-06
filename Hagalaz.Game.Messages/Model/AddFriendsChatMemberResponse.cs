namespace Hagalaz.Game.Messages.Model
{
    public class AddFriendsChatMemberResponse
    {
        public enum ResponseCode : byte
        {
            /// <summary>
            /// The attempt was successful.
            /// </summary>
            Success,
            /// <summary>
            /// An unexpected error has happend.
            /// </summary>
            Failed,
            /// <summary>
            /// The chat channel was not found.
            /// </summary>
            NotFound,
            /// <summary>
            /// The chat channel is currently full.
            /// </summary>
            Full,
            /// <summary>
            /// The user doesn't have the rank to join this channel.
            /// </summary>
            Unauthorized,
            /// <summary>
            /// The user has not the required rank.
            /// </summary>
            NotAllowed,
            /// <summary>
            /// The user is temporarily banned.
            /// </summary>
            Banned
        }
        
        public const byte Opcode = 8;
        
        public uint CharacterId { get; set; }
        public string ChatName { get; set; } = default!;
        public ResponseCode Response { get; set; }
    }
}