using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Defines a contract for a slayer task generator, which creates a new slayer assignment for a player based on a given set of parameters.
    /// </summary>
    public interface ISlayerTaskGenerator
    {
        /// <summary>
        /// Generates a new slayer task based on the provided parameters, such as the slayer master and the player's combat level.
        /// </summary>
        /// <param name="taskParams">The parameters for the slayer task generation.</param>
        /// <returns>A read-only list of <see cref="SlayerTaskResult"/>, typically containing a single new task.</returns>
        IReadOnlyList<SlayerTaskResult> GenerateTask(SlayerTaskParams taskParams);
    }
}