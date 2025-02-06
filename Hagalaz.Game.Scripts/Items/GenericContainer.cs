using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>
    /// Basic container implementation for generic purposes
    /// </summary>
    public class GenericContainer : BaseItemContainer
    {
        /// <summary>
        /// Get's called when generic container is updated.
        /// Slots can be null.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public delegate void UpdateCallback(HashSet<int>? slots);

        /// <summary>
        /// Contains update callback.
        /// </summary>
        private readonly UpdateCallback? _updateCallback;

        /// <summary>
        /// Construct's new generic container with given parameters.
        /// </summary>
        /// <param name="storageType">Type of the storage.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="updateCallback">The update callback.</param>
        public GenericContainer(StorageType storageType, short capacity, UpdateCallback? updateCallback = null)
            : base(storageType, capacity) =>
            _updateCallback = updateCallback;

        /// <summary>
        /// Get's called when specific colorSlots in container gets updated.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null)
        {
            if (_updateCallback != null)
                _updateCallback(slots);
        }
    }
}