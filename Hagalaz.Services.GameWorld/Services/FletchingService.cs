using System.Collections.Frozen;
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
    /// Serves Fletching recipes from an in-memory read model loaded once at startup, so item interactions on the game loop never wait on the database.
    /// The comma separated product columns cannot be translated to SQL, so entities are loaded first and mapped in memory.
    /// </summary>
    public class FletchingService : IFletchingService, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private FrozenDictionary<int, FletchingDto> _recipesByResourceId = FrozenDictionary<int, FletchingDto>.Empty;
        private FrozenDictionary<FletchingCategory, IReadOnlyList<FletchingDto>> _recipesByCategory = FrozenDictionary<FletchingCategory, IReadOnlyList<FletchingDto>>.Empty;

        public FletchingService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public FletchingDto? FindRecipe(FletchingCategory category, int itemId, int otherItemId) =>
            Match(category, itemId, otherItemId) ?? Match(category, otherItemId, itemId);

        public IReadOnlyList<FletchingDto> FindRecipes(FletchingCategory category) => _recipesByCategory.GetValueOrDefault(category, []);

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            var definitions = await scope.ServiceProvider.GetRequiredService<IFletchingDefinitionRepository>().FindAll().AsNoTracking().ToListAsync(cancellationToken);
            var recipes = mapper.Map<List<FletchingDto>>(definitions);
            _recipesByResourceId = recipes.ToFrozenDictionary(recipe => recipe.ResourceId);
            _recipesByCategory = recipes.GroupBy(recipe => recipe.Category)
                .ToFrozenDictionary(group => group.Key, group => (IReadOnlyList<FletchingDto>)group.ToArray());
        }

        private FletchingDto? Match(FletchingCategory category, int resourceId, int toolId) =>
            _recipesByResourceId.TryGetValue(resourceId, out var recipe) && recipe.Category == category && recipe.ToolId == toolId ? recipe : null;
    }
}
