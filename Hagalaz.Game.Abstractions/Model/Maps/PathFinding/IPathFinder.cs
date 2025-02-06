using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Model.Maps.PathFinding
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPathFinder
    {
        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="xOffset">The x offset.</param>
        /// <param name="yOffset">The y offset.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        bool CheckStep(int x, int y, int z, int xOffset, int yOffset, int size);
        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="checkDistance">The check distance.</param>
        /// <returns></returns>
        bool CheckStep(IVector3 location, int checkDistance);
        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        bool CheckStep(IVector3 location, DirectionFlag direction);
        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="target">The target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        IPath Find(IEntity entity, IGameObject target, bool moveNear);
        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="target">The target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        IPath Find(IEntity entity, IVector3 target, bool moveNear);
        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="target">The target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        IPath Find(IEntity entity, IEntity target, bool moveNear);
        /// <summary>
        /// Finds the specified start.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        IPath Find(IVector3 start, IVector3 end, bool moveNear);
        /// <summary>
        /// Finds the specified start.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        IPath Find(IVector3 start, IVector3 end, int selfSize, int targetSize, bool moveNear);
        /// <summary>
        /// Finds a path from the location to the end location.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="to">To.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="surroundings">The surroundings.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        IPath Find(IVector3 from, int selfSize, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings, bool moveNear);
    }
}
