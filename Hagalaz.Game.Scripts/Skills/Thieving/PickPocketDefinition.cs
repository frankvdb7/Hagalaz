namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    ///     Struct for PickPocketDefintion.
    /// </summary>
    public struct PickPocketDefinition
    {
        /// <summary>
        ///     The NPC IDs.
        /// </summary>
        public int[] NpcIDs;

        /// <summary>
        ///     The required level.
        /// </summary>
        public int RequiredLevel;

        /// <summary>
        ///     The experience.
        /// </summary>
        public double Experience;

        /// <summary>
        ///     The fail damage.
        /// </summary>
        public int FailDamage;

        /// <summary>
        ///     The extra loot thieving level.
        /// </summary>
        public int ExtraLootThievingLevel;

        /// <summary>
        ///     The extra loot agility level.
        /// </summary>
        public int ExtraLootAgilityLevel;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickPocketDefinition" /> struct.
        /// </summary>
        /// <param name="npcIDs">The NPC I ds.</param>
        /// <param name="requiredLevel">The required level.</param>
        /// <param name="experience">The experience.</param>
        /// <param name="failDamage">The fail damage.</param>
        /// <param name="extraLootThievingLevel">The extra loot thieving level.</param>
        /// <param name="extraLootAgilityLevel">The extra loot agility level.</param>
        public PickPocketDefinition(int[] npcIDs, int requiredLevel, double experience, int failDamage, int extraLootThievingLevel, int extraLootAgilityLevel)
        {
            NpcIDs = npcIDs;
            RequiredLevel = requiredLevel;
            Experience = experience;
            FailDamage = failDamage;
            ExtraLootThievingLevel = extraLootThievingLevel;
            ExtraLootAgilityLevel = extraLootAgilityLevel;
        }
    }
}