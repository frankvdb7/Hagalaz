using System;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Model.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class GroundItemBuilder : IGroundItemBuilder, IGroundItemOnGround, IGroundItemLocation, IGroundItemOptional, IGroundItemBuild
    {
        private IItem _item = default!;
        private ILocation _location = default!;
        private ICharacter? _owner = default!;
        private int? _respawnTicks = default!;
        private int? _ticks = default!;
        private bool _isRespawning;
        private readonly IOptions<GroundItemOptions> _groundItemOptions;
        private readonly IServiceProvider _serviceProvider;

        public GroundItemBuilder(IOptions<GroundItemOptions> groundItemOptions, IServiceProvider serviceProvider)
        {
            _groundItemOptions = groundItemOptions;
            _serviceProvider = serviceProvider;
        }

        public IGroundItemOnGround Create() => new GroundItemBuilder(_groundItemOptions, _serviceProvider);

        public IGroundItemLocation WithItem(IItem item)
        {
            _item = item;
            return this;
        }

        public IGroundItemLocation WithItem(Func<IItemBuilder, IItemBuild> itemBuilder)
        {
            var ib = _serviceProvider.GetRequiredService<IItemBuilder>();
            _item = itemBuilder(ib).Build();
            return this;
        }

        public IGroundItemOptional WithLocation(ILocation location)
        {
            _location = location;
            return this;
        }

        public IGroundItemOptional WithOwner(ICharacter owner)
        {
            _owner = owner;
            return this;
        }

        public IGroundItemOptional WithRespawnTicks(int respawnTicks)
        {
            _respawnTicks = respawnTicks;
            return this;
        }

        public IGroundItemOptional WithTicks(int ticks)
        {
            _ticks = ticks;
            return this;
        }

        public IGroundItemOptional AsRespawning()
        {
            _isRespawning = true;
            return this;
        }

        public IGroundItem Build()
        {
            var respawnTicks = _respawnTicks ??
                               (_item.ItemDefinition.Tradeable ? _groundItemOptions.Value.PublicTickTime : _groundItemOptions.Value.NonTradableTickTime);
            return new GroundItem(
                _item,
                _location,
                _owner,
                respawnTicks,
                _ticks ?? respawnTicks,
                _isRespawning);
        }

        public IGroundItem Spawn()
        {
            var groundItem = Build();
            var mapRegionService = _serviceProvider.GetRequiredService<IMapRegionService>();
            var mapRegion = mapRegionService.GetOrCreateMapRegion(groundItem.Location.RegionId, groundItem.Location.Dimension, true);
            mapRegion.Add(groundItem);
            return groundItem;
        }
    }
}