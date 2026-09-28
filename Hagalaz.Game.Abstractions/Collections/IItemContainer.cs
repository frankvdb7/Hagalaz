using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Describes the read-only shape and stacking behavior of an item container.</summary>
public interface IItemContainer : IContainer<IItem?>
{
    IItem? IContainer<IItem?>.this[int index] => ((IItemContainerStorageProvider)this).Storage[index];

    int IContainer<IItem?>.Capacity => ((IItemContainerStorageProvider)this).Storage.Capacity;

    IEnumerator<IItem?> IEnumerable<IItem?>.GetEnumerator() => ((IItemContainerStorageProvider)this).Storage.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IItemContainerStorageProvider)this).Storage.GetEnumerator();

    /// <summary>Gets the storage behavior of this container.</summary>
    StorageType Type => ((IItemContainerStorageProvider)this).Storage.Type;

    /// <summary>Gets the number of empty slots.</summary>
    int FreeSlots => ((IItemContainerStorageProvider)this).Storage.FreeSlots;

    /// <summary>Gets the number of occupied slots.</summary>
    int TakenSlots => ((IItemContainerStorageProvider)this).Storage.TakenSlots;
}
