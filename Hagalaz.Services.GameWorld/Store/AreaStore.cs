using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Model.Maps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuadTrees;

namespace Hagalaz.Services.GameWorld.Store
{
    public class AreaStore : IStartupService
    {
        private readonly Area _defaultArea;
        private readonly QuadTreeRect<Area> _areas;
        private readonly IAreaScriptProvider _scriptProvider;
        private readonly IServiceProvider _serviceProvider;

        public AreaStore(IAreaScriptProvider scriptProvider, IServiceProvider serviceProvider)
        {
            var min = Location.Zero;
            var max = Location.Create(ushort.MaxValue, ushort.MaxValue, byte.MaxValue, ushort.MaxValue);
            _defaultArea = new Area(0, "Default Area", min, max, false, false, true, true, scriptProvider.FindAreaScriptById(0));
            _areas = [_defaultArea];
            _scriptProvider = scriptProvider;
            _serviceProvider = serviceProvider;
        }

        public Area DefaultArea => _defaultArea;

        public QuadTreeRect<Area> Areas => _areas;

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var areaRepo = scope.ServiceProvider.GetRequiredService<IAreaRepository>();
            var areaEntities = await areaRepo.FindAll().AsNoTracking().ToArrayAsync();
            var areas = areaEntities.Select(area =>
                new Area(area.Id,
                    area.Name,
                    Location.Create(area.MinimumX,
                        area.MinimumY,
                        area.MinimumZ,
                        area.MinimumDimension),
                    Location.Create(area.MaximumX,
                        area.MaximumY,
                        area.MaximumZ,
                        area.MaximumDimension),
                    area.PvpAllowed == 1,
                    area.MulticombatAllowed == 1,
                    area.CannonAllowed == 1,
                    area.FamiliarAllowed == 1,
                    _scriptProvider.FindAreaScriptById(area.Id)));

            _areas.AddRange(areas);
        }
    }
}