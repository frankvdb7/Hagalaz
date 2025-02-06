using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMovement
    {
        /// <summary>
        /// Get's if character is moving.
        /// </summary>
        /// <value>
        ///   <c>true</c> if moving; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.NotSupportedException">Can't set Moving to true!</exception>
        bool Moving { get; }
        /// <summary>
        /// Get's if movement is locked.
        /// </summary>
        /// <value>
        ///   <c>true</c> if locked; otherwise, <c>false</c>.
        /// </value>
        bool Locked { get; }
        /// <summary>
        /// Contains boolean if creature moved on this cycle.
        /// </summary>
        /// <value>
        ///   <c>true</c> if moved; otherwise, <c>false</c>.
        /// </value>
        bool Moved { get; }
        /// <summary>
        /// Contains boolean if creature teleported on this cycle.
        /// </summary>
        /// <value>
        ///   <c>true</c> if teleported; otherwise, <c>false</c>.
        /// </value>
        bool Teleported { get; }
        /// <summary>
        /// Contains boolean which tells if temporary movement type
        /// is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [temporary movement type enabled]; otherwise, <c>false</c>.
        /// </value>
        bool TemporaryMovementTypeEnabled { get; }
        /// <summary>
        /// Contains creature movement type.
        /// </summary>
        /// <value>
        /// The type of the movement.
        /// </value>
        MovementType MovementType { get; set; }
        /// <summary>
        /// Contains temporary movement type.
        /// </summary>
        /// <value>
        /// The last type of the temporary movement.
        /// </value>
        MovementType LastTemporaryMovementType { get; }

        /// <summary>
        /// Teleports character to given location.
        /// </summary>
        /// <param name="teleport">Location where to teleport.</param>
        void Teleport(ITeleport teleport);
        /// <summary>
        /// Adds to queue.
        /// </summary>
        /// <param name="path">The path.</param>
        void AddToQueue(IPath path);
        /// <summary>
        /// Add's location to çreatures movement queue.
        /// Does not add anything to the queue, if a movement lock has been set.
        /// </summary>
        /// <param name="target">Where to move.</param>
        void AddToQueue(ILocation target);
        /// <summary>
        /// Clears movement queue.
        /// </summary>
        void ClearQueue();
        /// <summary>
        /// Processes character movement.
        /// </summary>
        void Tick();
        /// <summary>
        /// Resets movement data.
        /// </summary>
        void Reset();
        /// <summary>
        /// Lock's creature's movement.
        /// </summary>
        /// <param name="resetQueue">Wheter movement queue should be reset.</param>
        void Lock(bool resetQueue);
        /// <summary>
        /// Unlocks creature's movement.
        /// </summary>
        /// <param name="resetLock">if set to <c>true</c> [reset lock] completely.</param>
        void Unlock(bool resetLock);
    }
}
