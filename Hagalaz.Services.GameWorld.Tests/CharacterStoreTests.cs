using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.Options;
using NSubstitute;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterStoreTests
{
    [TestMethod]
    public async Task AddAsync_RejectsDuplicateMasterId()
    {
        var store = new CharacterStore(Options.Create(new GameServerOptions
        {
            ClientRevision = 1,
            ClientRevisionPatch = 0,
            AuthenticationToken = "test"
        }), new Hagalaz.Services.GameWorld.Services.RsTaskService(NullLogger<RsTaskService>.Instance), new CharacterLogoutState());
        var first = Substitute.For<ICharacter>();
        var second = Substitute.For<ICharacter>();
        first.MasterId.Returns(42u);
        second.MasterId.Returns(42u);

        Assert.IsTrue(await store.AddAsync(first));
        Assert.IsFalse(await store.AddAsync(second));
        Assert.AreEqual(1, await store.CountAsync());
    }

    [TestMethod]
    public async Task FindByIdAsyncAndFindByIndexAsync_ReturnExactCharacters()
    {
        var store = CreateStore();
        var first = Substitute.For<ICharacter>();
        var second = Substitute.For<ICharacter>();
        first.MasterId.Returns(42u);
        first.Index.Returns(3);
        second.MasterId.Returns(43u);
        second.Index.Returns(4);

        Assert.IsTrue(await store.AddAsync(first));
        Assert.IsTrue(await store.AddAsync(second));

        Assert.AreSame(first, await store.FindByIdAsync(42));
        Assert.AreSame(second, await store.FindByIndexAsync(second.Index));
        Assert.IsNull(await store.FindByIdAsync(99));
        Assert.IsNull(await store.FindByIndexAsync(-1));
        Assert.IsNull(await store.FindByIndexAsync(int.MaxValue));
    }

    [TestMethod]
    public async Task TryQueueTask_RejectsStaleInstanceAfterSlotIsReused()
    {
        var scheduler = new Hagalaz.Services.GameWorld.Services.RsTaskService(NullLogger<RsTaskService>.Instance);
        var store = new CharacterStore(
            Options.Create(new GameServerOptions
            {
                ClientRevision = 1,
                ClientRevisionPatch = 0,
                AuthenticationToken = "test"
            }),
            scheduler,
            new CharacterLogoutState());
        var first = Substitute.For<ICharacter>();
        first.MasterId.Returns(42u);
        var replacement = Substitute.For<ICharacter>();
        replacement.MasterId.Returns(42u);
        Assert.IsTrue(await store.AddAsync(first));
        Assert.IsTrue(store.Remove(first));
        Assert.IsTrue(await store.AddAsync(replacement));

        var staleTask = new RsTask(() => Assert.Fail("stale character task ran"), 1);
        var currentTaskRan = false;
        var currentTask = new RsTask(() => currentTaskRan = true, 1);

        Assert.IsFalse(store.TryQueueTask(first, staleTask));
        Assert.IsTrue(staleTask.IsCancelled);
        Assert.IsTrue(store.TryQueueTask(replacement, currentTask));
        scheduler.Tick();

        Assert.IsTrue(currentTaskRan);
    }

    [TestMethod]
    public async Task TryQueueTask_RejectsCharacterAfterLogoutIsClaimed()
    {
        var logoutState = new CharacterLogoutState();
        var store = new CharacterStore(
            Options.Create(new GameServerOptions
            {
                ClientRevision = 1,
                ClientRevisionPatch = 0,
                AuthenticationToken = "test"
            }),
            new Hagalaz.Services.GameWorld.Services.RsTaskService(NullLogger<RsTaskService>.Instance),
            logoutState);
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        Assert.IsTrue(await store.AddAsync(character));
        Assert.IsTrue(logoutState.TryBeginLogout(character, out _, out _));

        var task = new RsTask(() => Assert.Fail("logout character task ran"), 1);

        Assert.IsFalse(store.TryQueueTask(character, task));
        Assert.IsTrue(task.IsCancelled);
    }

    private static CharacterStore CreateStore() => new(Options.Create(new GameServerOptions
    {
        ClientRevision = 1,
        ClientRevisionPatch = 0,
        AuthenticationToken = "test"
    }), new Hagalaz.Services.GameWorld.Services.RsTaskService(NullLogger<RsTaskService>.Instance), new CharacterLogoutState());
}
