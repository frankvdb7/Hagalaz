using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Serves combat spell definitions from an in-memory read model loaded once at startup, so the synchronous widget lifecycle never waits on the database.
    /// </summary>
    public class CombatSpellCatalog : ICombatSpellCatalog, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;

        public CombatSpellCatalog(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IReadOnlyList<CombatSpellDto> CombatSpells { get; private set; } = [];

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            CombatSpells = await mapper
                .ProjectTo<CombatSpellDto>(scope.ServiceProvider.GetRequiredService<IMagicCombatSpellsRepository>().FindAll().AsNoTracking())
                .ToListAsync(cancellationToken);
        }
    }
}
