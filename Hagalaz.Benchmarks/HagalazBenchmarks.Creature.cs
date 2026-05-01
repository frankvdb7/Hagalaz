using BenchmarkDotNet.Attributes;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private BenchmarkCreature _creature1x1_1 = null!;
        private BenchmarkCreature _creature1x1_2 = null!;
        private BenchmarkCreature _creature3x3_1 = null!;
        private BenchmarkCreature _creature3x3_2 = null!;

        private void SetupCreatureBenchmarks()
        {
            var serviceScope = Substitute.For<IServiceScope>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceScope.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(ICreatureTaskService)).Returns(Substitute.For<ICreatureTaskService>());
            serviceProvider.GetService(typeof(IMapRegionService)).Returns(Substitute.For<IMapRegionService>());
            serviceProvider.GetService(typeof(IAreaService)).Returns(Substitute.For<IAreaService>());

            _creature1x1_1 = new BenchmarkCreature(serviceScope, 1);
            _creature1x1_2 = new BenchmarkCreature(serviceScope, 1);
            _creature3x3_1 = new BenchmarkCreature(serviceScope, 3);
            _creature3x3_2 = new BenchmarkCreature(serviceScope, 3);

            var loc1 = new Location(100, 100, 0, 0);
            var loc2 = new Location(115, 100, 0, 0); // Distance 15

            _creature1x1_1.SetLocation(loc1, firstUpdate: true);
            _creature1x1_2.SetLocation(loc2, firstUpdate: true);
            _creature3x3_1.SetLocation(loc1, firstUpdate: true);
            _creature3x3_2.SetLocation(loc2, firstUpdate: true);
        }

        [Benchmark]
        public bool CreatureWithinRange_1x1_WorstCase()
        {
            return _creature1x1_1.WithinRange(_creature1x1_2, 5);
        }

        [Benchmark]
        public bool CreatureWithinRange_3x3_WorstCase()
        {
            return _creature3x3_1.WithinRange(_creature3x3_2, 5);
        }

        private class BenchmarkCreature : Creature
        {
            private readonly int _size;
            public BenchmarkCreature(IServiceScope serviceScope, int size) : base(serviceScope)
            {
                _size = size;
                Movement = Substitute.For<IMovement>();
                Combat = Substitute.For<ICreatureCombat>();
                Viewport = Substitute.For<IViewport>();
            }

            public override int Size => _size;
            public override IPathFinder PathFinder => null!;
            public override bool CanDestroy() => true;
            public override bool CanSuspend() => true;
            public override void MovementTypeChanged(MovementType newtype) { }
            public override void OnDeath() { }
            public override void OnKilledBy(ICreature killer) { }
            public override void OnSpawn() { }
            public override void OnTargetKilled(ICreature target) { }
            public override bool Poison(short amount) => true;
            public override void Respawn() { }
            public override void TemporaryMovementTypeEnabled(MovementType type) { }
            public override void Interrupt(object source) { }
            public override bool ShouldBeRenderedFor(ICharacter viewer) => true;
            public override bool ShouldBeRenderedFor(INpc viewer) => true;

            protected override void AddToRegion(IMapRegion newRegion) { }
            protected override void ContentTick() { }
            protected override void CreatureFaced(ICreature? creature) { }
            protected override void GlowRendered(IGlow glow) { }
            protected override void HitBarRendered(IHitBar bar) { }
            protected override void HitSplatRendered(IHitSplat splat) { }
            protected override void NonstandardMovementRendered(IForceMovement movement) { }
            protected override void OnDestroy() { }
            protected override void OnLocationChange(ILocation? oldLocation) { }
            protected override void OnRegionChange() { }
            protected override void RemoveFromRegion(IMapRegion region) { }
            protected override void ResetTick() { }
            protected override void TextSpoken(string text) { }
            protected override void TurnedTo(int x, int y) { }
            protected override void UpdatesPrepareTick() { }
            protected override Task UpdateTick() => Task.CompletedTask;
        }
    }
}
