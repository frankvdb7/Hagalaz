namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILootItem : ILootObject
    {
        /// <summary>
        /// The item id.
        /// </summary>
        /// <value>The item id</value>
        int Id { get; }
        /// <summary>
        /// The minimum loot.
        /// </summary>
        /// <value>The minimum count.</value>
        int MinimumCount { get; }
        /// <summary>
        /// The maximum loot.
        /// </summary>
        /// <value>The maximum count.</value>
        int MaximumCount { get; }
    }
}
