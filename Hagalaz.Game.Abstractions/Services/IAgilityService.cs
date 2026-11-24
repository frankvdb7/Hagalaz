using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Agility content definitions, such as obstacle and course completion experience.
    /// Definitions are preloaded at startup, so lookups are synchronous and never perform I/O on the game loop.
    /// </summary>
    public interface IAgilityService
    {
        /// <summary>
        /// Finds an obstacle definition by the game object id of the obstacle.
        /// </summary>
        /// <param name="objectId">The game object id.</param>
        /// <returns>The <see cref="AgilityObstacleDto"/> if found; otherwise, <c>null</c>.</returns>
        AgilityObstacleDto? FindObstacleByObjectId(int objectId);

        /// <summary>
        /// Finds a course definition by its identity.
        /// </summary>
        /// <param name="courseId">The course identity.</param>
        /// <returns>The <see cref="AgilityCourseDto"/> if found; otherwise, <c>null</c>.</returns>
        AgilityCourseDto? FindCourseById(AgilityCourseId courseId);
    }
}
