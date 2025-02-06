using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    public record SlayerTaskResult(ISlayerTaskDefinition Definition, int KillCount);
}