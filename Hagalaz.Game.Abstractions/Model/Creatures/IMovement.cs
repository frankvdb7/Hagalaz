using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for managing a creature's movement, including pathfinding, teleportation, and state.
    /// </summary>
    public interface IMovement
    {
        /// <summary>
        /// Gets a value indicating whether the creature is currently moving.
        /// </summary>
        bool Moving { get; }

        /// <summary>
        /// Gets a value indicating whether the creature's movement is currently locked (e.g., by a stun or a script).
        /// </summary>
        bool Locked { get; }

        /// <summary>
        /// Gets a value indicating whether the creature has moved during the current game tick.
        /// </summary>
        bool Moved { get; }

        /// <summary>
        /// Gets a value indicating whether the creature has teleported during the current game tick.
        /// </summary>
        bool Teleported { get; }

        /// <summary>
        /// Gets a value indicating whether a temporary movement type is currently active.
        /// </summary>
        bool TemporaryMovementTypeEnabled { get; }

        /// <summary>
        /// Gets or sets the creature's current movement type (e.g., walking, running).
        /// </summary>
        MovementType MovementType { get; set; }

        /// <summary>
        /// Gets the last temporary movement type that was active.
        /// </summary>
        MovementType LastTemporaryMovementType { get; }

        /// <summary>
        /// Instantly moves the creature to a new location.
        /// </summary>
        /// <param name="teleport">The teleport request containing the destination and type.</param>
        void Teleport(ITeleport teleport);

        /// <summary>
        /// Adds a pre-calculated path to the movement queue.
        /// </summary>
        /// <param name="path">The path for the creature to follow.</param>
        void AddToQueue(IPath path);

        /// <summary>
        /// Calculates a path to the target location and adds it to the movement queue.
        /// </summary>
        /// <param name="target">The destination location.</param>
        void AddToQueue(ILocation target);

        /// <summary>
        /// Clears the current movement queue, stopping any planned movement.
        /// </summary>
        void ClearQueue();

        /// <summary>
        /// Processes a single game tick for the creature's movement, advancing it along its path.
        /// </summary>
        void Tick();

        /// <summary>
        /// Resets the movement state, clearing the queue and any movement-related flags.
        /// </summary>
        void Reset();

        /// <summary>
        /// Prevents the creature from moving.
        /// </summary>
        /// <param name="resetQueue">If set to <c>true</c>, the current movement queue will be cleared.</param>
        void Lock(bool resetQueue);

        /// <summary>
        /// Allows the creature to move again.
        /// </summary>
        /// <param name="resetLock">If set to <c>true</c>, any persistent movement lock is removed.</param>
        void Unlock(bool resetLock);
    }
}
