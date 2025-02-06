using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Events.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CreatureCollectionTests
    {
        [TestMethod]
        public void Empty_Collection_Test()
        {
            var collection = new CreatureCollection<CreatureMock>(2000);
            Assert.AreEqual(2000, collection.Capacity);
            Assert.AreEqual(0, collection.Count);
            Assert.AreEqual(collection[1000], null);
        }

        [TestMethod]
        public void Full_Collection_Test()
        {
            var collection = new CreatureCollection<CreatureMock>(4)
            {
                new CreatureMock(),
                new CreatureMock(),
                new CreatureMock()
            };
            Assert.AreEqual(3, collection.Count);
        }

        [TestMethod]
        public void Full_Collection_Remove_Add_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var creature4 = new CreatureMock();
            var creature5 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(4)
            {
                creature1,
                creature2,
                creature3,
                creature4
            };
            collection.Remove(creature2);
            collection.Add(creature5);
            Assert.AreEqual(4, collection.Count);
            Assert.AreEqual(creature1, collection[1]);
            Assert.AreEqual(creature3, collection[3]);
            Assert.AreEqual(creature4, collection[4]);
            Assert.AreEqual(creature5, collection[2]);
        }

        [TestMethod]
        public void Add_Single_Creature_Test()
        {
            var creature = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature
            };
            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(creature, collection[1]);
            Assert.AreEqual(creature.Index, 1);
        }

        [TestMethod]
        public void Add_Multiple_Creatures_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature1,
                creature2,
                creature3
            };
            Assert.AreEqual(3, collection.Count);
            Assert.AreEqual(creature1, collection[1]);
            Assert.AreEqual(creature1.Index, 1);
            Assert.AreEqual(creature2, collection[2]);
            Assert.AreEqual(creature2.Index, 2);
            Assert.AreEqual(creature3, collection[3]);
            Assert.AreEqual(creature3.Index, 3);
        }

        [TestMethod]
        public void Remove_Single_Creature_Test()
        {
            var creature = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature
            };
            collection.Remove(creature);
            Assert.AreEqual(0, collection.Count);
            Assert.AreEqual(null, collection[1]);
        }

        [TestMethod]
        public void Remove_Add_Multiple_Creatures_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature1,
                creature2
            };
            collection.Remove(creature1);
            collection.Add(creature3);
            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(creature2, collection[2]);
            Assert.AreEqual(creature3, collection[1]);
        }

        [TestMethod]
        public void Remove_Multiple_Creature_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature1,
                creature2,
                creature3
            };
            collection.Remove(creature1);
            collection.Remove(creature3);
            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(null, collection[1]);
            Assert.AreEqual(null, collection[3]);
            Assert.AreEqual(creature2, collection[2]);
        }

        [TestMethod]
        public void Enumerable_Single_Creature__Test()
        {
            var creature = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature
            };
            var foundCreature = collection.Where(creature => creature.Index == 1).FirstOrDefault();
            Assert.AreEqual(creature, foundCreature);
        }

        [TestMethod]
        public void Enumerable_Multiple_Creatures_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature1,
                creature2,
                creature3
            };
            var foundCreature = collection.Where(creature => creature.Index == 2).FirstOrDefault();
            Assert.AreEqual(creature2, foundCreature);
        }

        [TestMethod]
        public void Enumerable_Multiple_Creatures_Empty_Indices_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature1,
                creature2,
                creature3
            };
            collection.Remove(creature2);
            var foundCreature = collection.Where(creature => creature.Index == 3).FirstOrDefault();
            Assert.AreEqual(creature3, foundCreature);
        }

        [TestMethod]
        public async Task AsyncEnumerable_Multiple_Creatures_Empty_Indices_Test()
        {
            var creature1 = new CreatureMock();
            var creature2 = new CreatureMock();
            var creature3 = new CreatureMock();
            var collection = new CreatureCollection<CreatureMock>(2000)
            {
                creature1,
                creature2,
                creature3
            };
            collection.Remove(creature2);
            var foundCreature = await collection.ToAsyncEnumerable().Where(creature => creature.Index == 3).FirstOrDefaultAsync();
            Assert.AreEqual(creature3, foundCreature);
        }

        private class CreatureMock : ICreature
        {
            public int Index { get; set; }

            public string DisplayName => throw new NotImplementedException();

            public IForceMovement? RenderedNonstandardMovement => throw new NotImplementedException();

            public ICreature? FacedCreature => throw new NotImplementedException();

            public ILocation? LastLocation => throw new NotImplementedException();

            public IViewport Viewport => throw new NotImplementedException();

            public ICreatureCombat Combat => throw new NotImplementedException();

            public IArea Area => throw new NotImplementedException();

            public IMovement Movement => throw new NotImplementedException();

            public int TurnedToX => throw new NotImplementedException();

            public int TurnedToY => throw new NotImplementedException();

            public string? SpeakingText => throw new NotImplementedException();

            public IGlow? RenderedGlow => throw new NotImplementedException();

            public IReadOnlyList<IHitSplat> RenderedHitSplats => throw new NotImplementedException();

            public IReadOnlyList<IHitBar> RenderedHitBars => throw new NotImplementedException();

            public DirectionFlag FaceDirection => throw new NotImplementedException();

            public ILocation Location => throw new NotImplementedException();

            public IMapRegion Region => throw new NotImplementedException();

            public bool IsDestroyed => throw new NotImplementedException();

            public int Size => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public IPathFinder PathFinder => throw new NotImplementedException();

            public IServiceProvider ServiceProvider => throw new NotImplementedException();

            public IGameMediator Mediator => throw new NotImplementedException();

            public void AddState(IState state) => throw new NotImplementedException();
            public bool ApplyStandardState(IState state, StateType immunityType) => throw new NotImplementedException();
            public bool CanDestroy() => throw new NotImplementedException();
            public bool CanSuspend() => throw new NotImplementedException();
            public void Destroy() => throw new NotImplementedException();
            public void QueueForceMovement(IForceMovement movement) => throw new NotImplementedException();
            public void FaceCreature(ICreature creature) => throw new NotImplementedException();
            public IRsTaskHandle<TResult> QueueTask<TResult>(ITaskItem<TResult> task) => throw new NotImplementedException();
            public void FaceLocation(ILocation location, int tileSizeX = 1, int tileSizeY = 1) => throw new NotImplementedException();
            public bool Freeze(int ticks, int immunityTicks) => throw new NotImplementedException();
            public IEnumerable<IState> GetStates() => throw new NotImplementedException();
            public bool HasEventHandler<TEventType>() where TEventType : ICreatureEvent => throw new NotImplementedException();
            public bool HasState(StateType type) => throw new NotImplementedException();
            public void Interrupt(object source) => throw new NotImplementedException();
            public Task MajorClientPrepareUpdateTickAsync() => throw new NotImplementedException();
            public Task MajorClientUpdateResetTickAsync() => throw new NotImplementedException();
            public Task MajorClientUpdateTickAsync() => throw new NotImplementedException();
            public void MajorUpdateTick() => throw new NotImplementedException();
            public void OnDeath() => throw new NotImplementedException();
            public void OnKilledBy(ICreature killer) => throw new NotImplementedException();
            public Task OnRegistered() => throw new NotImplementedException();
            public void OnSpawn() => throw new NotImplementedException();
            public void OnTargetKilled(ICreature target) => throw new NotImplementedException();
            public bool Poison(short amount) => throw new NotImplementedException();
            public void QueueAnimation(IAnimation animation) => throw new NotImplementedException();
            public void QueueGlow(IGlow glow) => throw new NotImplementedException();
            public void QueueGraphic(IGraphic graphic) => throw new NotImplementedException();
            public void QueueHitBar(IHitBar hitBar) => throw new NotImplementedException();
            public void QueueHitSplat(IHitSplat splat) => throw new NotImplementedException();
            public IRsTaskHandle QueueTask(ITaskItem task) => throw new NotImplementedException();
            public void QueueTask(Func<Task> task) => throw new NotImplementedException();
            public EventHappened? RegisterEventHandler<TEventType>(EventHappened<TEventType> handler) where TEventType : ICreatureEvent => throw new NotImplementedException();
            public void RemoveState(StateType type) => throw new NotImplementedException();
            public void ResetFacing() => throw new NotImplementedException();
            public void Respawn() => throw new NotImplementedException();
            public bool ShouldBeRenderedFor(ICharacter viewer) => throw new NotImplementedException();
            public bool ShouldBeRenderedFor(INpc viewer) => throw new NotImplementedException();
            public void Speak(string text) => throw new NotImplementedException();
            public void Stun(int ticks) => throw new NotImplementedException();
            public bool UnregisterEventHandler<TEventType>(EventHappened handler) where TEventType : ICreatureEvent => throw new NotImplementedException();
            public bool WithinRange(ICreature otherCreature, int range) => throw new NotImplementedException();
            public bool WithinRange(ILocation otherLocation, int otherSize, int range) => throw new NotImplementedException();
        }
    }
}
