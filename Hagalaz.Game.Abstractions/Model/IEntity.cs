using System;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a game entity, which is any object that exists within the game world at a specific location.
    /// This is the base interface for characters, NPCs, game objects, and ground items.
    /// </summary>
    public interface IEntity : IRuneObject
    {
        /// <summary>
        /// Gets the current location of the entity in the game world.
        /// </summary>
        ILocation Location { get; }

        /// <summary>
        /// Gets the map region that the entity is currently in.
        /// </summary>
        [Obsolete("Use the IMapRegionService instead.")]
        IMapRegion Region { get; }

        /// <summary>
        /// Gets a value indicating whether the entity has been destroyed and removed from the game.
        /// </summary>
        bool IsDestroyed { get; }

        /// <summary>
        /// Gets the size of the entity in game tiles (e.g., a value of 1 means a 1x1 tile footprint).
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Determines whether this entity can be suspended (i.e., temporarily removed from active processing).
        /// </summary>
        /// <returns><c>true</c> if the entity can be suspended; otherwise, <c>false</c>.</returns>
        bool CanSuspend();

        /// <summary>
        /// Determines whether this entity can be destroyed.
        /// </summary>
        /// <returns><c>true</c> if the entity can be destroyed; otherwise, <c>false</c>.</returns>
        bool CanDestroy();

        /// <summary>
        /// Marks the entity for destruction and removal from the game world.
        /// </summary>
        void Destroy();

        /// <summary>
        /// A callback method that is executed when the entity is first spawned into the game world.
        /// </summary>
        void OnSpawn();
    }
}
