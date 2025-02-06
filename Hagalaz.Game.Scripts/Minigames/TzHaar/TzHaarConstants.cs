using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar
{
    public static class TzHaarConstants
    {
        public const string ProfileMinigamesTzhaarCaves = "minigames.tzhaar_caves";

        /// <summary>
        ///     The tzhaar mej jal npc id.
        /// </summary>
        public static readonly int TzhaarMejJal = 2617;

        /// <summary>
        ///     The cave entrance.
        /// </summary>
        public static readonly ILocation CaveEntrance = Location.Create(4608, 5129, 0, 0);

        /// <summary>
        ///     The cave center.
        /// </summary>
        public static readonly ILocation CaveCenter = Location.Create(4446, 5152, 0, 0);
    }
}