using System.Collections.Generic;
using System.IO;

namespace Hagalaz.Game.Abstractions.Model.Maps.PathFinding
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPath : IEnumerable<IVector2>
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="Path"/> is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if successful; otherwise, <c>false</c>.
        /// </value>
        bool Successful { get; set; }
        /// <summary>
        /// Gets a value indicating whether [moved near].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [moved near]; otherwise, <c>false</c>.
        /// </value>
        bool MovedNear { get; set; }
        /// <summary>
        /// Gets a value indicating whether [moved near destination].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [moved near destination]; otherwise, <c>false</c>.
        /// </value>
        bool MovedNearDestination { get; }
        /// <summary>
        /// Gets a value indicating whether [reached destination].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [reached destination]; otherwise, <c>false</c>.
        /// </value>
        bool ReachedDestination { get; }
        /// <summary>
        /// Adds the point.
        /// </summary>
        /// <param name="point">The point.</param>
        void Add(IVector2 point);
    }
}
