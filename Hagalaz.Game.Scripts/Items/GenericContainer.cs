using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>Basic container implementation for generic purposes.</summary>
    public class GenericContainer : IItemContainer, IItemContainerStorageOwner
    {
        public delegate void UpdateCallback(HashSet<int>? slots);

        private readonly UpdateCallback? _updateCallback;
        private readonly ItemContainerStorage _storage;

        ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
        void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);

        public StorageType Type => _storage.Type;
        public int FreeSlots => _storage.FreeSlots;
        public int TakenSlots => _storage.TakenSlots;

        public GenericContainer(StorageType storageType, short capacity, UpdateCallback? updateCallback = null)
        {
            _storage = new ItemContainerStorage(storageType, capacity);
            _updateCallback = updateCallback;
        }

        public void OnUpdate(HashSet<int>? slots = null) => _updateCallback?.Invoke(slots);
    }
}
