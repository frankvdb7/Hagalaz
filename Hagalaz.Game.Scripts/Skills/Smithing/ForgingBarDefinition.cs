using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public class ForgingBarEntry
    {
        /// <summary>
        ///     The product.
        /// </summary>
        public ItemDto Product;

        /// <summary>
        ///     The required smithing level
        /// </summary>
        public int RequiredSmithingLevel;

        /// <summary>
        ///     The required bar count
        /// </summary>
        public int RequiredBarCount;

        public ForgingBarEntry() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ForgingBarEntry" /> struct.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="requiredSmithingLevel">The required smithing level.</param>
        /// <param name="requiredBarCount">The required bar count.</param>
        public ForgingBarEntry(ItemDto product, int requiredSmithingLevel, int requiredBarCount)
        {
            Product = product;
            RequiredSmithingLevel = requiredSmithingLevel;
            RequiredBarCount = requiredBarCount;
        }
    }

    /// <summary>
    /// </summary>
    public class ForgingBarDefinition
    {
        /// <summary>
        ///     The experience.
        /// </summary>
        public double BaseSmithingExperience;

        /// <summary>
        ///     The entries.
        /// </summary>
        public ForgingBarEntry[] Entries = [];

        public ForgingBarDefinition() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ForgingBarDefinition" /> struct.
        /// </summary>
        /// <param name="baseSmithingExperience">The base smithing experience.</param>
        /// <param name="entries">The entries.</param>
        public ForgingBarDefinition(double baseSmithingExperience, ForgingBarEntry[] entries)
        {
            BaseSmithingExperience = baseSmithingExperience;
            Entries = entries;
        }
    }
}