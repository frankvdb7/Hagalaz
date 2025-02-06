using System;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEntity : IRuneObject
    {
        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        ILocation Location { get; }
        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <value>
        /// The region.
        /// </value>
        [Obsolete("Use the IMapRegionService instead.")]
        IMapRegion Region { get; }
        /// <summary>
        /// Wether the entity is destroyed.
        /// </summary>
        bool IsDestroyed { get; }
        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        int Size { get; }
        /// <summary>
        ///     Get's if entity can be suspended.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        bool CanSuspend();
        /// <summary>
        /// Get's if entity can be destroyed.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        bool CanDestroy();
        /// <summary>
        /// Destroys this entity.
        /// </summary>
        void Destroy();
        /// <summary>
        /// Get's called when entity spawns.
        /// </summary>
        void OnSpawn();
    }
}
