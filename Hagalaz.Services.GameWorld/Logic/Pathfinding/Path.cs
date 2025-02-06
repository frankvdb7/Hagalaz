using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Services.GameWorld.Logic.Pathfinding
{
    /// <summary>
    /// 
    /// </summary>
    public class Path : IPath
    {
        /// <summary>
        /// The points
        /// </summary>
        private readonly Queue<IVector2> _points = new();

        /// <summary>
        /// Gets a value indicating whether this <see cref="Path"/> is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if successful; otherwise, <c>false</c>.
        /// </value>
        public bool Successful { get; set; }

        /// <summary>
        /// Gets a value indicating whether [moved near].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [moved near]; otherwise, <c>false</c>.
        /// </value>
        public bool MovedNear { get; set; }

        /// <summary>
        /// Gets or sets the steps.
        /// </summary>
        /// <value>
        /// The steps.
        /// </value>
        public int Steps { get; set; } = -1;

        /// <summary>
        /// Gets a value indicating whether [reached destination].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [reached destination]; otherwise, <c>false</c>.
        /// </value>
        public bool ReachedDestination => Successful && !MovedNear && Steps == 0;

        /// <summary>
        /// Gets a value indicating whether [moved near destination].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [moved near destination]; otherwise, <c>false</c>.
        /// </value>
        public bool MovedNearDestination => Successful && MovedNear && Steps == 0;

        /// <summary>
        /// Adds the point.
        /// </summary>
        /// <param name="point">The point.</param>
        public void Add(IVector2 point) => _points.Enqueue(point);

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator() => _points.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<IVector2> IEnumerable<IVector2>.GetEnumerator() => _points.GetEnumerator();

        /// <summary>
        /// Peeks this instance.
        /// </summary>
        /// <returns></returns>
        public IVector2 Peek() => _points.Peek();
    }
}