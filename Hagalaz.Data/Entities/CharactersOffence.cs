using System;

namespace Hagalaz.Data.Entities
{
    public partial class CharactersOffence
    {
        /// <summary>
        ///     Defines the type of offence of the account.
        /// </summary>
        public enum OffenceTypeEnum : byte
        {
            /// <summary>
            /// </summary>
            None = 0,

            /// <summary>
            /// </summary>
            Muted = 1,

            /// <summary>
            /// </summary>
            Banned = 2
        }
        
        public uint Id { get; set; }
        public uint MasterId { get; set; }
        public OffenceTypeEnum OffenceType { get; set; }
        public DateTime Date { get; set; }
        public DateTime ExpireDate { get; set; }
        public uint ModeratorId { get; set; }
        public string Reason { get; set; } = null!;
        public uint AppealId { get; set; }
        public byte Expired { get; set; }

        public virtual Character Master { get; set; } = null!;
        public virtual Character Moderator { get; set; } = null!;
    }
}
