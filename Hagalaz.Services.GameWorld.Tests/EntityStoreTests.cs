using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class EntityStoreTests
{
    [TestMethod]
    public void AddAndResolve_UsesExactEntityReference()
    {
        var store = new EntityStore();
        var entity = EntityTestFactory.Create<ICharacter>();

        var handle = store.Add(entity);

        Assert.AreNotEqual(default, handle);
        Assert.AreEqual(handle, entity.Handle);
        Assert.IsTrue(store.TryResolve(handle, out var resolved));
        Assert.AreSame(entity, resolved);
    }

    [TestMethod]
    public void Remove_InvalidatesHandleAndSlotReuseAdvancesGeneration()
    {
        var store = new EntityStore();
        var original = EntityTestFactory.Create<ICharacter>();
        var replacement = EntityTestFactory.Create<INpc>();

        var originalHandle = store.Add(original);
        Assert.IsTrue(store.Remove(original));
        Assert.IsFalse(store.TryResolve(originalHandle, out _));

        var replacementHandle = store.Add(replacement);

        Assert.AreEqual(originalHandle.Slot, replacementHandle.Slot);
        Assert.AreNotEqual(originalHandle.Generation, replacementHandle.Generation);
        Assert.IsFalse(store.TryResolve(originalHandle, out _));
        Assert.IsTrue(store.TryResolve(replacementHandle, out var resolved));
        Assert.AreSame(replacement, resolved);
    }

    [TestMethod]
    public void EntityService_RejectsWrongResolvedType()
    {
        var store = new EntityStore();
        var service = new EntityService(store);
        var npc = EntityTestFactory.Create<INpc>();
        var handle = store.Add(npc);

        Assert.IsFalse(service.TryResolve<ICharacter>(handle, out _));
        Assert.IsTrue(service.TryResolve<INpc>(handle, out var resolved));
        Assert.AreSame(npc, resolved);
    }

    [TestMethod]
    public void CharacterAndNpcWithSameProtocolIndexHaveIndependentHandles()
    {
        var store = new EntityStore();
        var character = EntityTestFactory.Create<ICharacter>();
        var npc = EntityTestFactory.Create<INpc>();
        character.Index.Returns(1);
        npc.Index.Returns(1);

        var characterHandle = store.Add(character);
        var npcHandle = store.Add(npc);

        Assert.AreNotEqual(characterHandle, npcHandle);
        Assert.IsTrue(store.TryResolve(characterHandle, out var resolvedCharacter));
        Assert.IsTrue(store.TryResolve(npcHandle, out var resolvedNpc));
        Assert.AreSame(character, resolvedCharacter);
        Assert.AreSame(npc, resolvedNpc);
    }
}
