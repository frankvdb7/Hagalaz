namespace Hagalaz.Characters.Messages.Model
{
    public record ItemCollectionDto
    {
        public required IList<ItemDto> Bank { get; init; }
        public required IList<ItemDto> Inventory { get; init; }
        public required IList<ItemDto> FamiliarInventory { get; init; }
        public required IList<ItemDto> Equipment { get; init; }
        public required IList<ItemDto> Rewards { get; init; }
        public required IList<ItemDto> MoneyPouch { get; init; }
    }
}
