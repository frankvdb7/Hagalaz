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
        public IItem? this[int index] => _storage[index];
        public int Capacity => _storage.Capacity;
        public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Add(IItem item)
        {
            if (!_storage.TryAdd(item, out var slots)) return false;
            OnUpdate(slots);
            return true;
        }
        public bool Add(int slot, IItem item)
        {
            if (!_storage.TryAdd(slot, item, out var slots)) return false;
            OnUpdate(slots);
            return true;
        }
        public void AddAndRemoveFrom(IItemContainer source) => ItemContainerTransfer.AddAndRemoveFrom(this, source);
        public IItem? GetById(int id) => _storage.GetById(id);
        public int Remove(IItem item, int preferredSlot = -1, bool update = true)
        {
            var removed = _storage.Remove(item, preferredSlot, out var slots);
            if (removed > 0 && update) OnUpdate(slots);
            return removed;
        }
        public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
        public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
        public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(null); }
        public bool AddRange(IEnumerable<IItem?> items)
        {
            if (!_storage.TryAddRange(items, out var slots)) return false;
            OnUpdate(slots);
            return true;
        }
        public bool Contains(int id, int count) => _storage.Contains(id, count);
        public bool Contains(int id) => _storage.Contains(id);
        public int GetCount(IItem item) => _storage.GetCount(item);
        public int GetCountById(int id) => _storage.GetCountById(id);
        public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
        public void Sort() { _storage.Sort(); OnUpdate(null); }
        public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
        public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
        public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
        public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(null); }

        public GenericContainer(StorageType storageType, short capacity, UpdateCallback? updateCallback = null)
        {
            _storage = new ItemContainerStorage(storageType, capacity);
            _updateCallback = updateCallback;
        }

        public void OnUpdate(HashSet<int>? slots = null) => _updateCallback?.Invoke(slots);
    }
}
