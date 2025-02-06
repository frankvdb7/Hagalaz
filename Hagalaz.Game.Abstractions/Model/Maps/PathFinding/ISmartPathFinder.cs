namespace Hagalaz.Game.Abstractions.Model.Maps.PathFinding
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISmartPathFinder : IPathFinder
    {
        /// <summary>
        /// Finds the adjacent.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        IPath? FindAdjacent(IEntity entity);
    }
}
