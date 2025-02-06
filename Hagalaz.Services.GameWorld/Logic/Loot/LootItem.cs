using Hagalaz.Game.Abstractions.Logic.Loot;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    /// <summary>
    /// This represents an item in the loot table.
    /// </summary>
    public class LootItem : ILootItem
    {
        /// <summary>
        /// The item id.
        /// </summary>
        /// <value>The item id</value>
        public int Id { get; }

        /// <summary>
        /// The minimum loot.
        /// </summary>
        /// <value>The minimum count.</value>
        public int MinimumCount { get; }

        /// <summary>
        /// The maximum loot.
        /// </summary>
        /// <value>The maximum count.</value>
        public int MaximumCount { get; }

        /// <summary>
        /// Gets or sets the probability for this object to be (part of) the result
        /// </summary>
        public double Probability { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LootItem" /> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="minimumCount">The minimum count.</param>
        /// <param name="maximumCount">The maximum count.</param>
        /// <param name="probability">The probability.</param>
        /// <param name="always">if set to <c>true</c> [always].</param>
        public LootItem(int id, int minimumCount, int maximumCount, double probability, bool always)
        {
            Id = id;
            MinimumCount = minimumCount;
            MaximumCount = maximumCount;
            Probability = probability;
            Always = always;
            Enabled = true;
        }

        /// <summary>
        /// Gets or sets whether this object will always be part of the result set
        /// (Probability is ignored when this flag is set to true)
        /// </summary>
        public bool Always { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="LootItem" /> is enabled.
        /// Only enabled entries can be part of the result of a ILootTable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; init; }
    }
}