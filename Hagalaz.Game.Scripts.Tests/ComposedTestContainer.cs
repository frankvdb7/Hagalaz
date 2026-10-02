using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using NSubstitute;
using System.Threading;

namespace Hagalaz.Game.Scripts.Tests;

internal class ComposedTestContainer : IInventoryContainer, IRewardContainer
{
    private readonly ItemContainer _items;
    protected ItemContainer Container => _items;
    public IItemContainer Items => _items;
    public Action? OnUpdateAction { get; set; }
    public int UpdateCount { get; private set; }
    public bool CanAcquireMutationLockFromOtherThread()
    {
        var entered = new ManualResetEventSlim();
        var acquisition = new Thread(() => _items.ExecuteUnderMutationLock(entered.Set))
        {
            IsBackground = true
        };
        acquisition.Start();
        if (!entered.Wait(TimeSpan.FromSeconds(1)))
        {
            acquisition.Join(TimeSpan.FromSeconds(1));
            return false;
        }

        acquisition.Join();
        entered.Dispose();
        return true;
    }

    public ComposedTestContainer(int capacity) : this(StorageType.Normal, capacity) { }
    public ComposedTestContainer(StorageType type, int capacity, int countToResetTo = -1, bool publishItemChanges = true) =>
        _items = new ItemContainer(type, capacity, publishItemChanges ? OnUpdate : null, countToResetTo);

    public static IItem CreateTestItem(int id, int count = 1)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(false);
        item.ItemDefinition.Returns(definition);
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            ReferenceEquals(item, call.ArgAt<IItem>(0)));
        item.Clone().Returns(item);
        item.Clone(Arg.Any<int>()).Returns(item);
        return item;
    }

    public bool DropItem(IItem item) => false;
    public int Claim(IItem item, int count) => 0;
    public void OnUpdate(HashSet<int>? changedSlots = null)
    {
        UpdateCount++;
        OnUpdateAction?.Invoke();
    }

    public void SetItem(int slot, IItem item) => _items.Replace(slot, item);
}
