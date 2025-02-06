using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public class SmeltingBarDefinition
    {
        /// <summary>
        ///     The required ores.
        /// </summary>
        public ItemDto[] RequiredOres;

        /// <summary>
        ///     The smelting level.
        /// </summary>
        public int RequiredSmithingLevel;

        /// <summary>
        ///     The experience.
        /// </summary>
        public double SmithingExperience;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmeltingBarDefinition" /> struct.
        /// </summary>
        /// <param name="requiredOres">The required ores.</param>
        /// <param name="requiredSmithingLevel">The required smithing level.</param>
        /// <param name="smithingExperience">The smithing experience.</param>
        public SmeltingBarDefinition(ItemDto[] requiredOres, int requiredSmithingLevel, double smithingExperience)
        {
            RequiredOres = requiredOres;
            RequiredSmithingLevel = requiredSmithingLevel;
            SmithingExperience = smithingExperience;
        }
    }
}