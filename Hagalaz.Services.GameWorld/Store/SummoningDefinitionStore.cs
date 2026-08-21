using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Store
{
    /// <summary>
    /// Holds summoning definitions required by synchronous character registration paths.
    /// </summary>
    public sealed class SummoningDefinitionStore : ISummoningDefinitionStore, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMapper _mapper;
        private IReadOnlyDictionary<int, SummoningDto> _definitionsByNpcId = new Dictionary<int, SummoningDto>();

        public SummoningDefinitionStore(IServiceProvider serviceProvider, IMapper mapper)
        {
            _serviceProvider = serviceProvider;
            _mapper = mapper;
        }

        public SummoningDto? FindByNpcId(int npcId) =>
            _definitionsByNpcId.TryGetValue(npcId, out var definition) ? definition : null;

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISummoningDefinitionRepository>();
            var definitions = await _mapper
                .ProjectTo<SummoningDto>(repository.FindAll().AsNoTracking())
                .ToListAsync(cancellationToken);

            _definitionsByNpcId = definitions.ToDictionary(definition => definition.NpcId);
        }
    }
}
