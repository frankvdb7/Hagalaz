using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Logic.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Store
{
    public class FishingStore : IStartupService
    {
        private readonly IServiceProvider _serviceProvider;

        public List<FishingSpotTable> FishingSpots { get; private set; } = [];

        public FishingStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var definitions = await scope.ServiceProvider.GetRequiredService<IFishingSpotDefinitionRepository>().FindAll().AsNoTracking().ToListAsync();
            FishingSpots = definitions.Select(f =>
                    new FishingSpotTable((int)f.Id,
                        "Fishing spot",
                        f.SkillsFishingFishDefinitions.Select(fish => new FishingLoot
                            {
                                Id = fish.ItemId,
                                Probability = (double)fish.Probability,
                                RequiredLevel = fish.RequiredLevel,
                                FishingExperience = (double)fish.Experience
                            })
                            .ToList<IFishingLoot>(),
                        [
                            new FishingLootModifier
                            {
                                RequiredLevel = (int)f.MinimumLevel
                            }
                        ])
                    {
                        MaxResultCount = 1,
                        RandomizeResultCount = false,
                        BaitId = f.BaitId ?? 0,
                        BaseCatchChance = (double)f.BaseCatchChance,
                        ClickType = Enum.Parse<NpcClickType>(f.ClickType),
                        ExhaustChance = (double)f.ExhaustChance,
                        MinimumLevel = f.MinimumLevel,
                        RespawnTime = (double)f.RespawnTime,
                        NpcIds = f.SkillsFishingSpotNpcDefinitions.Select(n => (int)n.NpcId).ToHashSet(),
                        RequiredTool = new FishingToolDto
                        {
                            CastAnimationId = f.Tool.CastAnimationId ?? -1, FishAnimationId = f.Tool.FishAnimationId, ItemId = f.Tool.ItemId
                        }
                    })
                .ToList();
        }
    }
}