using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    public interface IFishingSpotTable : IRandomTable<IFishingLoot>
    {
        int BaitId { get; }
        double BaseCatchChance { get; }
        NpcClickType ClickType { get; }
        double ExhaustChance { get; }
        int MinimumLevel { get; }
        IReadOnlySet<int> NpcIds { get; }
        FishingToolDto RequiredTool { get; }
        double RespawnTime { get; }
    }
}