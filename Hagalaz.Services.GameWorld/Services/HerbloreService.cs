using System.Linq;
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
    /// Serves Herblore potion definitions from an in-memory read model loaded once at startup, so item interactions on the game loop never wait on the database.
    /// Herb definitions are queried from the repository on demand.
    /// </summary>
    public class HerbloreService : IHerbloreService, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private PotionDto[] _potions = [];

        public HerbloreService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<IReadOnlyList<HerbDto>> FindAllHerbs()
        {
            using var scope = _serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            return await mapper.ProjectTo<HerbDto>(scope.ServiceProvider.GetRequiredService<IHerbloreRepository>().FindAll()).ToListAsync();
        }

        public async Task<HerbDto?> FindGrimyHerbById(int id)
        {
            using var scope = _serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            return await mapper.ProjectTo<HerbDto>(scope.ServiceProvider.GetRequiredService<IHerbloreRepository>().FindGrimyHerbById(id)).FirstOrDefaultAsync();
        }

        public IReadOnlyList<PotionDto> FindPotions(PotionKind kind) => _potions.Where(potion => potion.Kind == kind).ToArray();

        public PotionDto? FindPotionByPrimaryItemId(int primaryItemId) =>
            _potions.FirstOrDefault(potion => potion.Kind == PotionKind.Standard && potion.PrimaryItemIds.Contains(primaryItemId));

        public PotionDto? FindPotionBySecondaryItemId(PotionKind kind, int unfinishedPotionId, int secondaryItemId) =>
            _potions.FirstOrDefault(potion => potion.Kind == kind && potion.UnfinishedPotionId == unfinishedPotionId && potion.SecondaryItemIds.Contains(secondaryItemId));

        /// <summary>
        /// Loads the potion definitions ordered by potion id, which keeps the first match for primary ingredients shared by several recipes stable.
        /// The comma separated ingredient columns cannot be translated to SQL, so entities are loaded first and mapped in memory.
        /// </summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            var definitions = await scope.ServiceProvider.GetRequiredService<IHerblorePotionDefinitionRepository>().FindAll()
                .AsNoTracking()
                .OrderBy(definition => definition.PotionId)
                .ToListAsync(cancellationToken);
            _potions = mapper.Map<PotionDto[]>(definitions);
        }
    }
}
