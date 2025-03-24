using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Logic.Loot;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{

    public class SlayerMasterTable : RandomTableBase<ISlayerTaskDefinition>, ISlayerMasterTable
    {
        public required int BaseSlayerRewardPoints { get; init; }

        public SlayerMasterTable()
        {
            MaxResultCount = 1;
            RandomizeResultCount = false;
        }
    }
}