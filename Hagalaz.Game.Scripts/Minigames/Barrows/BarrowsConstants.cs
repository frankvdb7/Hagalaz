using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    /// </summary>
    public static class BarrowsConstants
    {
        public const string MinigamesBarrows = "minigames.barrows";

        /// <summary>
        ///     The probability barrow brother killed modifier.
        /// </summary>
        public static readonly double ProbabilityBarrowBrotherKilledModifier = 1.10;

        /// <summary>
        ///     The ahrim index
        /// </summary>
        public static readonly int AhrimIndex = 0;

        /// <summary>
        ///     The dharok index
        /// </summary>
        public static readonly int DharokIndex = 1;

        /// <summary>
        ///     The guthan index
        /// </summary>
        public static readonly int GuthanIndex = 2;

        /// <summary>
        ///     The karil index
        /// </summary>
        public static readonly int KarilIndex = 3;

        /// <summary>
        ///     The torag index
        /// </summary>
        public static readonly int ToragIndex = 4;

        /// <summary>
        ///     The verac index
        /// </summary>
        public static readonly int VeracIndex = 5;

        /// <summary>
        ///     The akrisae index
        /// </summary>
        public static readonly int AkrisaeIndex = 6;

        /// <summary>
        ///     The barrow brother Ids
        /// </summary>
        public static int[] BarrowBrotherIDs =
        [
            2025, // ahrim
            2026, // dharok
            2027, // guthan
            2028, // karil
            2029, // torag
            2030, // verac
            14297 // akrisae
        ];

        /// <summary>
        ///     The crypt character spawns
        /// </summary>
        public static ILocation[] CryptCharacterSpawns =
        [
            Location.Create(3535, 9712, 0, 0), // North-west config 452 64
            Location.Create(3569, 9711, 0, 0), // North-east config 452 128
            Location.Create(3534, 9677, 0, 0), // South-west config 452 256
            Location.Create(3568, 9677, 0, 0) // South-east config 452 512
        ];

        /// <summary>
        ///     The barrow brother heads.
        /// </summary>
        public static readonly int[][] BarrowBrotherHeads = [
            [4761, 4763, 4765, 4767, 4769, 4771], [4761, 4762, 4763, 4764, 4765, 4766, 4767, 4768, 4769, 4770, 4771]
        ];

        /// <summary>
        ///     The dharok spades
        /// </summary>
        public static readonly ILocation DharokSpades = Location.Create(3575, 3298, 0);

        /// <summary>
        ///     The verac spades
        /// </summary>
        public static readonly ILocation VeracSpades = Location.Create(3557, 3298, 0);

        /// <summary>
        ///     The ahrim spades
        /// </summary>
        public static readonly ILocation AhrimSpades = Location.Create(3567, 3288, 0);

        /// <summary>
        ///     The torag spades
        /// </summary>
        public static readonly ILocation ToragSpades = Location.Create(3554, 3282, 0);

        /// <summary>
        ///     The karil spades
        /// </summary>
        public static readonly ILocation KarilSpades = Location.Create(3564, 3277, 0);

        /// <summary>
        ///     The guthan spades
        /// </summary>
        public static readonly ILocation GuthanSpades = Location.Create(3576, 3281, 0);
    }
}