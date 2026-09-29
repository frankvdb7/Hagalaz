using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>Basic container implementation for generic purposes.</summary>
    public class GenericContainer
    {
        public delegate void UpdateCallback(HashSet<int>? slots);

        public IItemContainer Items { get; }

        public GenericContainer(StorageType storageType, short capacity, UpdateCallback? updateCallback = null) =>
            Items = new ItemContainer(storageType, capacity, slots => updateCallback?.Invoke(slots));
    }
}
