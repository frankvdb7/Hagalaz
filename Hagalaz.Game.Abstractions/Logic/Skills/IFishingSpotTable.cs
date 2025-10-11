using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Defines a contract for a fishing spot's loot table, which contains the potential fish that can be caught
    /// and the parameters governing the fishing process at this spot.
    /// </summary>
    public interface IFishingSpotTable : IRandomTable<IFishingLoot>
    {
        /// <summary>
        /// Gets the item ID of the bait required to fish at this spot. A value of -1 indicates no bait is required.
        /// </summary>
        int BaitId { get; }

        /// <summary>
        /// Gets the base probability of successfully catching a fish on each attempt.
        /// </summary>
        double BaseCatchChance { get; }

        /// <summary>
        /// Gets the type of click interaction required to start fishing at this spot (e.g., Net, Bait).
        /// </summary>
        NpcClickType ClickType { get; }

        /// <summary>
        /// Gets the probability that the fishing spot will be exhausted and disappear after a successful catch.
        /// </summary>
        double ExhaustChance { get; }

        /// <summary>
        /// Gets the minimum fishing level required to attempt to fish at this spot.
        /// </summary>
        int MinimumLevel { get; }

        /// <summary>
        /// Gets a read-only set of NPC IDs that represent this type of fishing spot in the game world.
        /// </summary>
        IReadOnlySet<int> NpcIds { get; }

        /// <summary>
        /// Gets the tool required to fish at this spot (e.g., a specific type of fishing rod or net).
        /// </summary>
        FishingToolDto RequiredTool { get; }

        /// <summary>
        /// Gets the time in seconds it takes for an exhausted fishing spot to respawn.
        /// </summary>
        double RespawnTime { get; }
    }
}