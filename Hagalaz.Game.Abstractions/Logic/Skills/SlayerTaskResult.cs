using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Represents the result of a slayer task generation, containing the assigned task and the number of creatures to kill.
    /// </summary>
    /// <param name="Definition">The definition of the assigned slayer task.</param>
    /// <param name="KillCount">The number of the assigned creature that the player must kill.</param>
    public record SlayerTaskResult(ISlayerTaskDefinition Definition, int KillCount);
}