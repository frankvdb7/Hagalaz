namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// Represents the result of a loot generation, containing a specific item and the quantity that was dropped.
    /// </summary>
    /// <param name="Item">The loot item that was generated.</param>
    /// <param name="Count">The number of the generated items.</param>
    /// <typeparam name="T">The type of the loot item, which must implement <see cref="ILootItem"/>.</typeparam>
    public record LootResult<T>(T Item, int Count) where T : ILootItem;
}