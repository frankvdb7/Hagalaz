using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Services.Characters.Services.Model
{
    public record Item
    {
        public int ItemId { get; init; }
        public int Count { get; init; }
        public int SlotId { get; init; }
        public ItemContainerType ContainerType { get; init; }
        public string? ExtraData { get; init; }
    }
}
