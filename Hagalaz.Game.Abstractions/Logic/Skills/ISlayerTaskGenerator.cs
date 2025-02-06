using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    public interface ISlayerTaskGenerator
    {
        IReadOnlyList<SlayerTaskResult> GenerateTask(SlayerTaskParams taskParams);
    }
}