using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Describes the operations and shape of an item container.</summary>
public interface IItemContainer : IContainer<IItem?>
{
    StorageType Type { get; }

    int FreeSlots { get; }

    int TakenSlots { get; }

    bool Add(IItem item);

    bool Add(int slot, IItem item);

    void AddAndRemoveFrom(ItemContainer source);

    IItem? GetById(int id);

    int Remove(IItem item, int preferredSlot = -1, bool update = true);

    bool TryRemoveExact(IItem item, int preferredSlot = -1);

    void Replace(int slot, IItem item);

    void Swap(int fromSlot, int toSlot);

    void Move(int fromSlot, int toSlot);

    bool AddRange(IEnumerable<IItem?> items);

    bool Contains(int id, int count);

    bool Contains(int id);

    int GetCount(IItem item);

    int GetCountById(int id);

    int GetInstanceSlot(IItem instance);

    void Sort();

    int GetSlotByItem(IItem item, bool ignoreCount = true);

    bool HasSpaceFor(IItem item);

    bool HasSpaceForRange(IEnumerable<IItem?> items);

    void Clear(bool update);
}
