using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Logic.Loot;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    /// <summary>
    /// 
    /// </summary>
    public class SlayerMasterTable : RandomTableBase<ISlayerTaskDefinition>, ISlayerMasterTable
    {
        public required int NpcId { get; init; }

        /// <summary>
        /// Contains the base slayer reward points.
        /// </summary>
        public required int BaseSlayerRewardPoints { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlayerMasterTable" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="modifiers"></param>
        public SlayerMasterTable(int id, string name, List<IRandomObjectModifier> modifiers) : base(id, name, [], modifiers)
        {
            MaxResultCount = 1;
            RandomizeResultCount = false;
        }
    }
}