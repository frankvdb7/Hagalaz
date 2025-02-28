using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Logic.Skills;
using Hagalaz.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Store
{
    public class SlayerStore : IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private List<SlayerMasterTable> _slayerMasterTables = [];

        public IReadOnlyList<SlayerMasterTable> SlayerMasterTables => _slayerMasterTables;

        public SlayerStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var slayerMasterDefinitionRepository = scope.ServiceProvider.GetRequiredService<ISlayerMasterDefinitionRepository>();
            var slayerTaskDefinitionRepository = scope.ServiceProvider.GetRequiredService<ISlayerTaskDefinitionRepository>();
            var modifiers = new List<IRandomObjectModifier>
            {
                new SlayerTaskModifier()
            };
            _slayerMasterTables = await slayerMasterDefinitionRepository.FindAll()
                .Select(c => new SlayerMasterTable((int)c.NpcId,
                    c.Name,
                    modifiers)
                {
                    NpcId = (int)c.NpcId, BaseSlayerRewardPoints = (int)c.BaseSlayerRewardPoints
                })
                .ToListAsync();
            var slayerTasks = await slayerTaskDefinitionRepository.FindAll()
                .Select(t => new SlayerTaskDefinition(t.Id,
                    t.Name,
                    (int)t.SlayerMasterId,
                    StringUtilities.SelectIntFromString(t.NpcIds).ToArray(),
                    (int)t.LevelRequirement,
                    t.CombatRequirement,
                    t.MinimumCount,
                    t.MaximumCount,
                    (int)t.CoinCount))
                .ToListAsync();
            foreach (var masterTable in _slayerMasterTables)
            {
                foreach (var task in slayerTasks.Where(t => t.SlayerMasterId == masterTable.NpcId))
                {
                    masterTable.AddEntry(task);
                }
            }
        }
    }
}