using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Describes the operations and shape of an item container.</summary>
public interface IItemContainer : IReadOnlyItemContainer
{
    IItemContainerMutationBoundary Mutations { get; }

    StorageType Type { get; }

    bool Add(IItem item);

    bool Add(int slot, IItem item);

    int Remove(IItem item, int preferredSlot = -1, bool update = true);

    bool TryRemoveExact(IItem item, int preferredSlot = -1);

    void Replace(int slot, IItem item);

    void Swap(int fromSlot, int toSlot);

    void Move(int fromSlot, int toSlot);

    bool AddRange(IEnumerable<IItem?> items);

    void Sort();

    void Clear(bool update);
}
