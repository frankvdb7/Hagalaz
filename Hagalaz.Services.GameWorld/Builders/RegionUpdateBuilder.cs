using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class RegionUpdateBuilder : IRegionUpdateBuilder, IRegionUpdateOptional, IRegionUpdateBuild, IRegionUpdateLocation
    {
        private readonly IMapRegionService _mapRegionService;
        private IGraphic? _graphic;
        private ILocation _location = default!;

        public RegionUpdateBuilder(IMapRegionService mapRegionService)
        {
            _mapRegionService = mapRegionService;
        }

        public IRegionUpdateLocation Create() => new RegionUpdateBuilder(_mapRegionService);

        public IRegionUpdateBuild WithGraphic(IGraphic graphic)
        {
            _graphic = graphic;
            return this;
        }

        public IRegionPartUpdate Build()
        {
            if (_graphic != null)
            {
                return new DrawGraphicUpdate(_graphic, _location);
            }
            return new NoopRegionUpdate(_location);
        }

        public IRegionPartUpdate Queue()
        {
            var update = Build();
            var region = _mapRegionService.GetOrCreateMapRegion(_location.RegionId, _location.Dimension, true);
            region.QueueUpdate(update);
            return update;
        }

        public IRegionUpdateOptional WithLocation(ILocation location)
        {
            _location = location;
            return this;
        }
    }
}