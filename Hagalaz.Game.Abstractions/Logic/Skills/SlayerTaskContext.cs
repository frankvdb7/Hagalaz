using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Represents the context for a slayer task generation, containing the proposed task and the character it is for.
    /// This context can be used by modifiers to alter the task based on the character's properties.
    /// </summary>
    /// <param name="Task">The slayer task definition being processed.</param>
    public record SlayerTaskContext(ISlayerTaskDefinition Task) : RandomObjectContext(Task)
    {
        /// <summary>
        /// Gets the character for whom the slayer task is being generated.
        /// </summary>
        public required ICharacter Character { get; init; }
    }
}