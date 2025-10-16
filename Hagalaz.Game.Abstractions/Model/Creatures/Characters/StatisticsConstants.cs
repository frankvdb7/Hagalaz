namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Provides constant values related to character statistics and skills.
    /// </summary>
    public static class StatisticsConstants
    {
        /// <summary>
        /// The total number of skills in the game.
        /// </summary>
        public const int SkillsCount = 25;
        /// <summary>
        /// The maximum amount of experience a character can have in a single skill.
        /// </summary>
        public const int MaximumExperience = 200000000;
        /// <summary>
        /// The maximum amount of run energy a character can have.
        /// </summary>
        public const int MaximumRunEnergy = 100;
        /// <summary>
        /// The maximum amount of special attack energy a character can have.
        /// </summary>
        public const int MaximumSpecialEnergy = 1000;
        /// <summary>
        /// The skill ID for Attack.
        /// </summary>
        public const int Attack = 0;
        /// <summary>
        /// The skill ID for Defence.
        /// </summary>
        public const int Defence = 1;
        /// <summary>
        /// The skill ID for Strength.
        /// </summary>
        public const int Strength = 2;
        /// <summary>
        /// The skill ID for Constitution.
        /// </summary>
        public const int Constitution = 3;
        /// <summary>
        /// The skill ID for Ranged.
        /// </summary>
        public const int Ranged = 4;
        /// <summary>
        /// The skill ID for Prayer.
        /// </summary>
        public const int Prayer = 5;
        /// <summary>
        /// The skill ID for Magic.
        /// </summary>
        public const int Magic = 6;
        /// <summary>
        /// The skill ID for Cooking.
        /// </summary>
        public const int Cooking = 7;
        /// <summary>
        /// The skill ID for Woodcutting.
        /// </summary>
        public const int Woodcutting = 8;
        /// <summary>
        /// The skill ID for Fletching.
        /// </summary>
        public const int Fletching = 9;
        /// <summary>
        /// The skill ID for Fishing.
        /// </summary>
        public const int Fishing = 10;
        /// <summary>
        /// The skill ID for Firemaking.
        /// </summary>
        public const int Firemaking = 11;
        /// <summary>
        /// The skill ID for Crafting.
        /// </summary>
        public const int Crafting = 12;
        /// <summary>
        /// The skill ID for Smithing.
        /// </summary>
        public const int Smithing = 13;
        /// <summary>
        /// The skill ID for Mining.
        /// </summary>
        public const int Mining = 14;
        /// <summary>
        /// The skill ID for Herblore.
        /// </summary>
        public const int Herblore = 15;
        /// <summary>
        /// The skill ID for Agility.
        /// </summary>
        public const int Agility = 16;
        /// <summary>
        /// The skill ID for Thieving.
        /// </summary>
        public const int Thieving = 17;
        /// <summary>
        /// The skill ID for Slayer.
        /// </summary>
        public const int Slayer = 18;
        /// <summary>
        /// The skill ID for Farming.
        /// </summary>
        public const int Farming = 19;
        /// <summary>
        /// The skill ID for Runecrafting.
        /// </summary>
        public const int Runecrafting = 20;
        /// <summary>
        /// The skill ID for Construction.
        /// </summary>
        public const int Construction = 21;
        /// <summary>
        /// The skill ID for Hunter.
        /// </summary>
        public const int Hunter = 22;
        /// <summary>
        /// The skill ID for Summoning.
        /// </summary>
        public const int Summoning = 23;
        /// <summary>
        /// The skill ID for Dungeoneering.
        /// </summary>
        public const int Dungeoneering = 24;
        /// <summary>
        /// An array of skill names, ordered by their skill ID.
        /// </summary>
        public static readonly string[] SkillNames =
        [
            "Attack", "Defence", "Strength", "Constitution", "Range", "Prayer", "Magic", "Cooking", "Woodcutting", "Fletching", "Fishing", "Firemaking", "Crafting", "Smithing", "Mining", "Herblore", "Agility", "Thieving", "Slayer", "Farming", "Runecrafting", "Construction", "Hunter", "Summoning", "Dungeoneering"
        ];
        /// <summary>
        /// An array of client configuration IDs used to make a skill's icon flash upon leveling up.
        /// </summary>
        public static readonly int[] SkillFlashFlags =
        [
            1, 4, 2, 64, 8, 16, 32, 32768, 131072, 2048, 16384, 65536, 1024, 8192, 4096, 256, 128, 512, 524288, 1048576, 262144, 2097152, 4194304, 8388608, 16777216
        ];
        /// <summary>
        /// An array that maps server-side skill IDs to their corresponding client-side IDs.
        /// </summary>
        public static readonly int[] ClientSkillIDs =
        [
            0, 4, 1, 5, 2, 6, 3, 15, 17, 18, 14, 16, 10, 13, 12, 8, 7, 9, 19, 20, 11, 21, 22, 23, 24
        ];
    }
}