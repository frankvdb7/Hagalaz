using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    public record SlayerTaskContext(ISlayerTaskDefinition Task) : RandomObjectContext(Task)
    {
        public required ICharacter Character { get; init; }
    }
}