namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    public record LootResult<T>(T Item, int Count) where T : ILootItem;
}