using System;
using System.Threading;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapRegionPartTests
    {
        [TestMethod]
        public void QueueUpdate_RejectsNull()
        {
            var part = CreatePart();

            Assert.ThrowsExactly<ArgumentNullException>(() => part.QueueUpdate(null!));
        }

        [TestMethod]
        public void QueueUpdate_CanRunConcurrentlyWithClearUpdates()
        {
            var part = CreatePart();
            var update = new TestRegionPartUpdate();
            using var start = new Barrier(5);

            Parallel.Invoke(
                () => QueueUpdates(part, update, start),
                () => QueueUpdates(part, update, start),
                () => QueueUpdates(part, update, start),
                () => QueueUpdates(part, update, start),
                () => ClearUpdates(part, start));
        }

        [TestMethod]
        public void SendUpdates_UsesSnapshotWhenFilteringClearsQueue()
        {
            var mapper = Substitute.For<IMapper>();
            var part = new MapRegionPart(mapper, Substitute.For<IGroundItemBuilder>());
            var character = Substitute.For<ICharacter>();
            character.Viewport.Returns(Substitute.For<IViewport>());
            character.Session.Returns(Substitute.For<IGameSession>());
            var filterCalled = false;
            var update = new TestRegionPartUpdate(() =>
            {
                filterCalled = true;
                part.ClearUpdates();
            });

            part.QueueUpdate(update);
            part.SendUpdates(character);

            Assert.IsTrue(filterCalled);
        }

        private static MapRegionPart CreatePart() => new(
            Substitute.For<IMapper>(),
            Substitute.For<IGroundItemBuilder>());

        private static void QueueUpdates(MapRegionPart part, IRegionPartUpdate update, Barrier start)
        {
            start.SignalAndWait();
            for (var i = 0; i < 10_000; i++)
            {
                part.QueueUpdate(update);
            }
        }

        private static void ClearUpdates(MapRegionPart part, Barrier start)
        {
            start.SignalAndWait();
            for (var i = 0; i < 10_000; i++)
            {
                part.ClearUpdates();
            }
        }

        private sealed class TestRegionPartUpdate : IRegionPartUpdate
        {
            private readonly Action? _onCanUpdateFor;

            public TestRegionPartUpdate(Action? onCanUpdateFor = null) => _onCanUpdateFor = onCanUpdateFor;

            public ILocation Location { get; } = new Location(3200, 3200, 0, 0);

            public bool CanUpdateFor(ICharacter character)
            {
                _onCanUpdateFor?.Invoke();
                return true;
            }

            public void OnUpdatedFor(ICharacter character)
            {
            }
        }
    }
}
