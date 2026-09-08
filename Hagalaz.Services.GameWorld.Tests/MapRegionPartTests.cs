using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionPartTests
{
    [TestMethod]
    public void QueueUpdate_RejectsNull()
    {
        var part = CreatePart(out _);

        Assert.ThrowsExactly<ArgumentNullException>(() => part.QueueUpdate(null!));
    }

    [TestMethod]
    public void SendUpdates_UsesOnlyThePreparedBuffer()
    {
        var part = CreatePart(out _);
        var character = CreateCharacter();
        var update = new TestRegionPartUpdate();

        part.QueueUpdate(update);
        part.SendUpdates(character);
        Assert.AreEqual(0, update.CanUpdateForCalls);

        part.PrepareUpdatesForTick();
        part.SendUpdates(character);
        Assert.AreEqual(1, update.CanUpdateForCalls);

        part.CompleteUpdateTick();
        part.SendUpdates(character);
        Assert.AreEqual(1, update.CanUpdateForCalls);
    }

    [TestMethod]
    public void UpdatesQueuedAfterPrepare_AreDeferredUntilTheNextTick()
    {
        var part = CreatePart(out _);
        var character = CreateCharacter();
        var currentTick = new TestRegionPartUpdate();
        var nextTick = new TestRegionPartUpdate();

        part.QueueUpdate(currentTick);
        part.PrepareUpdatesForTick();
        part.QueueUpdate(nextTick);
        part.SendUpdates(character);

        Assert.AreEqual(1, currentTick.CanUpdateForCalls);
        Assert.AreEqual(0, nextTick.CanUpdateForCalls);

        part.CompleteUpdateTick();
        part.PrepareUpdatesForTick();
        part.SendUpdates(character);

        Assert.AreEqual(1, nextTick.CanUpdateForCalls);
    }

    [TestMethod]
    public void APreparedBuffer_IsSentToEveryCharacterBeforeCompletion()
    {
        var part = CreatePart(out _);
        var firstCharacter = CreateCharacter();
        var secondCharacter = CreateCharacter();
        var first = new TestRegionPartUpdate();
        var second = new TestRegionPartUpdate();

        part.QueueUpdate(first);
        part.QueueUpdate(second);
        part.QueueUpdate(second);
        part.PrepareUpdatesForTick();

        part.SendUpdates(firstCharacter);
        part.SendUpdates(secondCharacter);

        Assert.AreEqual(2, first.CanUpdateForCalls);
        Assert.AreEqual(2, second.CanUpdateForCalls);
    }

    [TestMethod]
    public async Task SendUpdates_DoesNotHoldTheUpdateLockDuringCallbacks()
    {
        var part = CreatePart(out _);
        var character = CreateCharacter();
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prepared = new TestRegionPartUpdate(() =>
        {
            callbackStarted.TrySetResult();
            releaseCallback.Task.GetAwaiter().GetResult();
        });
        var queuedDuringCallback = new TestRegionPartUpdate();

        part.QueueUpdate(prepared);
        part.PrepareUpdatesForTick();
        var sendTask = Task.Run(() => part.SendUpdates(character));
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        part.QueueUpdate(queuedDuringCallback);
        releaseCallback.TrySetResult();
        await sendTask.WaitAsync(TimeSpan.FromSeconds(1));

        part.CompleteUpdateTick();
        part.PrepareUpdatesForTick();
        part.SendUpdates(character);

        Assert.AreEqual(1, queuedDuringCallback.CanUpdateForCalls);
    }

    [TestMethod]
    public void CompleteUpdateTick_DoesNotLosePendingUpdatesWhenCompletionRunsConcurrently()
    {
        var part = CreatePart(out _);
        var character = CreateCharacter();
        var pending = Enumerable.Range(0, 256).Select(_ => new TestRegionPartUpdate()).ToArray();

        part.PrepareUpdatesForTick();
        Parallel.Invoke(
            () =>
            {
                foreach (var update in pending)
                {
                    part.QueueUpdate(update);
                }
            },
            () =>
            {
                for (var i = 0; i < 256; i++)
                {
                    part.CompleteUpdateTick();
                }
            });

        part.PrepareUpdatesForTick();
        part.SendUpdates(character);

        Assert.IsTrue(pending.All(update => update.CanUpdateForCalls == 1));
    }

    private static MapRegionPart CreatePart(out IMapper mapper)
    {
        mapper = Substitute.For<IMapper>();
        mapper.Map<RaidoMessage>(Arg.Any<object>()).Returns(Substitute.For<RaidoMessage>());
        return new MapRegionPart(mapper, Substitute.For<Hagalaz.Game.Abstractions.Builders.GroundItem.IGroundItemBuilder>());
    }

    private static ICharacter CreateCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.Viewport.Returns(Substitute.For<IViewport>());
        character.Session.Returns(Substitute.For<IGameSession>());
        return character;
    }

    private sealed class TestRegionPartUpdate : IRegionPartUpdate
    {
        private readonly Action? _onCanUpdateFor;

        public TestRegionPartUpdate(Action? onCanUpdateFor = null) => _onCanUpdateFor = onCanUpdateFor;

        public ILocation Location { get; } = new Location(3200, 3200, 0, 0);

        public int CanUpdateForCalls { get; private set; }

        public bool CanUpdateFor(ICharacter character)
        {
            CanUpdateForCalls++;
            _onCanUpdateFor?.Invoke();
            return true;
        }

        public void OnUpdatedFor(ICharacter character)
        {
        }
    }
}
