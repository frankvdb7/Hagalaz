using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Logic.Loot;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    /// <summary>
    /// Data for a fishing spot.
    /// </summary>
    public class FishingSpotTable : RandomTableBase<IFishingLoot>, IFishingSpotTable
    {
        /// <summary>
        /// The rate at which the fishing spot exhausts.
        /// </summary>
        public required double ExhaustChance { get; init; }

        /// <summary>
        /// The time at which the fishing spot becomes unexhausted.
        /// </summary>
        public required double RespawnTime { get; init; }

        /// <summary>
        /// The minimal level required to fish at the location.
        /// </summary>
        public required int MinimumLevel { get; init; }

        /// <summary>
        /// The fishing tool
        /// </summary>
        public required FishingToolDto RequiredTool { get; init; }

        /// <summary>
        /// The bait identifier
        /// </summary>
        public required int BaitId { get; init; }

        /// <summary>
        /// The click type
        /// </summary>
        public required NpcClickType ClickType { get; init; }

        /// <summary>
        /// Gets the base catch chance.
        /// </summary>
        /// <value>
        /// The base catch chance.
        /// </value>
        public required double BaseCatchChance { get; init; }

        /// <summary>
        /// Gets the NPC ids.
        /// </summary>
        /// <value>
        /// The NPC ids.
        /// </value>
        public required IReadOnlySet<int> NpcIds { get; init; }
    }
}