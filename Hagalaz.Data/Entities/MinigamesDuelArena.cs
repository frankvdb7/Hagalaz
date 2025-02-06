using System;

namespace Hagalaz.Data.Entities
{
    [Obsolete("Use Profile instead")]
    public partial class MinigamesDuelArena
    {
        public uint MasterId { get; set; }
        public string PreviousRules { get; set; } = null!;
        public string FavouriteRules { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
    }
}
