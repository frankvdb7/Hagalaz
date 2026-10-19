using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides indexed and enumerable read access to items and item-specific queries.</summary>
public interface IReadOnlyItemContainer : IContainer<IItem?>
{
    int FreeSlots { get; }

    int TakenSlots { get; }

    IItem? GetById(int id);

    int GetCount(IItem item);

    int GetCountById(int id);

    int GetInstanceSlot(IItem instance);

    int GetSlotByItem(IItem item, bool ignoreCount = true);

    bool Contains(int id);

    bool Contains(int id, int count);

    bool HasSpaceFor(IItem item);

    bool HasSpaceForRange(IEnumerable<IItem?> items);
}
