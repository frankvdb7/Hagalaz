using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Agility content definitions, such as obstacle and course completion experience.
    /// </summary>
    public interface IAgilityService
    {
        /// <summary>
        /// Finds an obstacle definition by the game object id of the obstacle.
        /// </summary>
        /// <param name="objectId">The game object id.</param>
        /// <returns>A task whose result is the <see cref="AgilityObstacleDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<AgilityObstacleDto?> FindObstacleByObjectId(int objectId);

        /// <summary>
        /// Finds a course definition by its identity.
        /// </summary>
        /// <param name="courseId">The course identity.</param>
        /// <returns>A task whose result is the <see cref="AgilityCourseDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<AgilityCourseDto?> FindCourseById(AgilityCourseId courseId);
    }
}
