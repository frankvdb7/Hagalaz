using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Items;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class CasketTests
{
    [TestMethod]
    public async Task ItemClickedInInventory_WhenCharacterIsCancelledDuringLookup_LeavesCasketAndLootUntouched()
    {
        var lootService = Substitute.For<ILootService>();
        var lookupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookup = new TaskCompletionSource<ILootTable?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lootService.FindItemLootTable(1).Returns(_ =>
        {
            lookupStarted.TrySetResult(true);
            return lookup.Task;
        });

        var character = Substitute.For<ICharacter>();
        var item = Substitute.For<IItem>();
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.GetInstanceSlot(item).Returns(0);
        character.Inventory.Returns(inventory);
        var operation = CaptureQueuedOperation(character);

        var casket = new Casket(lootService);
        casket.ItemClickedInInventory(ComponentClickType.LeftClick, item, character);

        using var cancellation = new CancellationTokenSource();
        var operationTask = operation(cancellation.Token);
        await lookupStarted.Task;

        cancellation.Cancel();
        lookup.SetResult(Substitute.For<ILootTable>());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => operationTask);

        inventory.DidNotReceive().Remove(Arg.Any<IItem>(), Arg.Any<int>(), Arg.Any<bool>());
        character.DidNotReceive().SendChatMessage("and found some glorious loot!");
    }

    [TestMethod]
    public async Task ItemClickedInInventory_WhenLookupCompletes_ConsumesCasketAndGrantsLoot()
    {
        var lootService = Substitute.For<ILootService>();
        var table = Substitute.For<ILootTable>();
        lootService.FindItemLootTable(1).Returns(Task.FromResult<ILootTable?>(table));

        var lootItem = Substitute.For<ILootItem>();
        lootItem.Id.Returns(995);
        var generatedItem = Substitute.For<IItem>();
        var lootGenerator = Substitute.For<ILootGenerator>();
        lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>())
            .Returns(new[] { new LootResult<ILootItem>(lootItem, 1) });

        var itemBuilder = Substitute.For<IItemBuilder>();
        var itemId = Substitute.For<IItemId>();
        var itemOptional = Substitute.For<IItemOptional>();
        itemBuilder.Create().Returns(itemId);
        itemId.WithId(995).Returns(itemOptional);
        itemOptional.WithCount(1).Returns(itemOptional);
        ((IItemBuild)itemOptional).Build().Returns(generatedItem);

        var character = Substitute.For<ICharacter>();
        var item = Substitute.For<IItem>();
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.GetInstanceSlot(item).Returns(3);
        inventory.Remove(item, 3).Returns(1);
        inventory.Add(generatedItem).Returns(true);
        character.Inventory.Returns(inventory);
        character.ServiceProvider.Returns(new ServiceCollection()
            .AddSingleton<ILootGenerator>(lootGenerator)
            .AddSingleton<IItemBuilder>(itemBuilder)
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .BuildServiceProvider());
        var operation = CaptureQueuedOperation(character);

        new Casket(lootService).ItemClickedInInventory(ComponentClickType.LeftClick, item, character);
        await operation(CancellationToken.None);

        inventory.Received(1).Remove(item, 3);
        inventory.Received(1).Add(generatedItem);
        lootGenerator.Received(1).GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>());
    }

    private static Func<CancellationToken, Task> CaptureQueuedOperation(ICharacter character)
    {
        Func<CancellationToken, Task>? operation = null;
        character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(value => operation = value))
            .Returns(Substitute.For<IRsTaskHandle>());
        return cancellationToken =>
            (operation ?? throw new InvalidOperationException("The casket operation was not queued."))(cancellationToken);
    }
}
