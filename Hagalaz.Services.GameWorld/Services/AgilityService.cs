using System.Collections.Frozen;
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
    /// Serves Agility definitions from an in-memory read model loaded once at startup, so obstacle callbacks on the game loop never wait on the database.
    /// </summary>
    public class AgilityService : IAgilityService, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private FrozenDictionary<int, AgilityObstacleDto> _obstacles = FrozenDictionary<int, AgilityObstacleDto>.Empty;
        private FrozenDictionary<AgilityCourseId, AgilityCourseDto> _courses = FrozenDictionary<AgilityCourseId, AgilityCourseDto>.Empty;

        public AgilityService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public AgilityObstacleDto? FindObstacleByObjectId(int objectId) => _obstacles.GetValueOrDefault(objectId);

        public AgilityCourseDto? FindCourseById(AgilityCourseId courseId) => _courses.GetValueOrDefault(courseId);

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            var obstacles = await mapper
                .ProjectTo<AgilityObstacleDto>(scope.ServiceProvider.GetRequiredService<IAgilityObstacleDefinitionRepository>().FindAll())
                .ToListAsync(cancellationToken);
            var courses = await mapper
                .ProjectTo<AgilityCourseDto>(scope.ServiceProvider.GetRequiredService<IAgilityCourseDefinitionRepository>().FindAll())
                .ToListAsync(cancellationToken);
            _obstacles = obstacles.ToFrozenDictionary(o => o.ObjectId);
            _courses = courses.ToFrozenDictionary(c => c.Id);
        }
    }
}
