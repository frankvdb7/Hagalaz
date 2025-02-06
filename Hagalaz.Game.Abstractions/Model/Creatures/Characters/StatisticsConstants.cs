namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public static class StatisticsConstants
    {

        /// <summary>
        /// The number of skills in game as of revision 742.
        /// </summary>
        public const int SkillsCount = 25;

        /// <summary>
        /// The maxmimum amount of experience a character can have in a skill.
        /// </summary>
        public const int MaximumExperience = 200000000;

        /// <summary>
        /// The maximum run energy.
        /// </summary>
        public const int MaximumRunEnergy = 100;

        /// <summary>
        /// The maximum special enenergy.
        /// </summary>
        public const int MaximumSpecialEnergy = 1000;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Attack = 0;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Defence = 1;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Strength = 2;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Constitution = 3;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Ranged = 4;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Prayer = 5;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Magic = 6;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Cooking = 7;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Woodcutting = 8;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Fletching = 9;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Fishing = 10;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Firemaking = 11;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Crafting = 12;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Smithing = 13;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Mining = 14;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Herblore = 15;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Agility = 16;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Thieving = 17;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Slayer = 18;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Farming = 19;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Runecrafting = 20;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Construction = 21;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Hunter = 22;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Summoning = 23;

        /// <summary>
        /// Id's of the skills.
        /// </summary>
        public const int Dungeoneering = 24;

        /// <summary>
        /// Names of the skills
        /// </summary>
        public static readonly string[] SkillNames =
        {
            "Attack", "Defence", "Strength", "Constitution", "Range", "Prayer", "Magic", "Cooking", "Woodcutting", "Fletching", "Fishing", "Firemaking", "Crafting", "Smithing", "Mining", "Herblore", "Agility", "Thieving", "Slayer", "Farming", "Runecrafting", "Construction", "Hunter", "Summoning", "Dungeoneering"
        };

        /// <summary>
        /// Skill flash config IDS.
        /// </summary>
        public static readonly int[] SkillFlashFlags =
        {
            1, 4, 2, 64, 8, 16, 32, 32768, 131072, 2048, 16384, 65536, 1024, 8192, 4096, 256, 128, 512, 524288, 1048576, 262144, 2097152, 4194304, 8388608, 16777216,
        };

        /// <summary>
        /// The tracked skill ids.
        /// </summary>
        public static readonly int[] ClientSkillIDs = new int[]
        {
            0, 4, 1, 5, 2, 6, 3, 15, 17, 18, 14, 16, 10, 13, 12, 8, 7, 9, 19, 20, 11, 21, 22, 23, 24
        };
    }
}