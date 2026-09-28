using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Logic.Characters;

internal static class HydratedItemDtoExtensions
{
    public static (int Slot, IItem Item) ToStorageEntry(this HydratedItemDto entry, IItemBuilder itemBuilder) =>
        (entry.SlotId, itemBuilder.Create().WithId(entry.ItemId).WithCount(entry.Count)
            .WithExtraData(entry.ExtraData ?? string.Empty).Build());

    public static HydratedItemDto[] ToHydratedItems(this ItemContainerStorage storage) =>
        storage.Select((item, slot) => (item, slot)).Where(entry => entry.item != null)
            .Select(entry => new HydratedItemDto(entry.item!.Id, entry.item.Count, entry.slot,
                entry.item.SerializeExtraData())).ToArray();
}
