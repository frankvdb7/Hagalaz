using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class MoneyPouchContainer : BaseItemContainer, IMoneyPouchContainer, IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        /// The previous count
        /// </summary>
        private int _previousCount;

        /// <summary>
        /// Contains the money count.
        /// </summary>
        public int Count => this[0]!.Count;

        /// <summary>
        /// Gets the examine.
        /// </summary>
        /// <value>
        /// The examine.
        /// </value>
        public string Examine
        {
            get
            {
                if (Count == 1) return "Your money pouch currently contains 1 coin.";
                return "Your money pouch currently contains " + StringUtilities.FormatNumber(Count) + " coins.";
            }
        }

        /// <summary>
        /// Contstructs a container for character inventories.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        public MoneyPouchContainer(ICharacter owner)
            : base(StorageType.Normal, 1)
        {
            _owner = owner;
            _itemBuilder = owner.ServiceProvider.GetRequiredService<IItemBuilder>();
            var coins = _itemBuilder.Create().WithId(995).WithCount(0).Build();
            Items = [coins];
            CountToResetTo = 0;
        }

        /// <summary>
        /// Adds the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool Add(int count)
        {
            var totalCount = (ulong)count + (ulong)Count;
            if (totalCount > int.MaxValue)
            {
                var remainingSpace = int.MaxValue - Count;
                if (remainingSpace > 0)
                {
                    _previousCount = Count;
                    SendMoneyPouchChangedMessage(remainingSpace);
                    Add(_itemBuilder.Create().WithId(995).WithCount(remainingSpace).Build());
                }

                var inventoryCount = count - remainingSpace;
                return count <= 0 || _owner.Inventory.Add(_itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build());
            }

            _previousCount = Count;
            SendMoneyPouchChangedMessage(count);
            return Add(_itemBuilder.Create().WithId(995).WithCount(count).Build());
        }

        /// <summary>
        /// Removes the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Remove(int count)
        {
            if (count > Count)
            {
                var remaining = count - Count;
                var inventoryCount = _owner.Inventory.GetCountById(995);
                if (remaining > inventoryCount) remaining = inventoryCount;
                _previousCount = Count;
                var removed = 0;
                if (Count > 0) removed += Remove(_itemBuilder.Create().WithId(995).WithCount(Count).Build(), 0);
                if (remaining > 0) removed += _owner.Inventory.Remove(_itemBuilder.Create().WithId(995).WithCount(remaining).Build());
                SendMoneyPouchChangedMessage(-removed);
                return removed;
            }

            _previousCount = Count;
            SendMoneyPouchChangedMessage(-count);
            return Remove(_itemBuilder.Create().WithId(995).WithCount(count).Build(), 0);
        }

        /// <summary>
        /// Sends the money pouch changed message.
        /// </summary>
        /// <param name="changeCount">The change count.</param>
        private void SendMoneyPouchChangedMessage(int changeCount)
        {
            switch (changeCount)
            {
                case < 0: _owner.SendChatMessage(StringUtilities.FormatNumber(-changeCount) + " coins have been removed from your money pouch."); break;
                case > 0: _owner.SendChatMessage(StringUtilities.FormatNumber(changeCount) + " coins have been added to your money pouch."); break;
            }
        }

        /// <summary>
        /// Adds from inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool AddFromInventory(int count)
        {
            var totalCount = (ulong)count + (ulong)Count;
            if (totalCount > int.MaxValue)
            {
                count = int.MaxValue - Count;
                _owner.SendChatMessage(GameStrings.MoneyPouchFull);
            }

            if (count <= 0) return false;
            var remove = _itemBuilder.Create().WithId(995).WithCount(count).Build();
            var removed = _owner.Inventory.Remove(remove);
            return removed > 0 && Add(removed);
        }

        /// <summary>
        /// Moves to inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool MoveToInventory(int count)
        {
            _previousCount = Count;
            var remove = _itemBuilder.Create().WithId(995).WithCount(count).Build();
            if (!_owner.Inventory.HasSpaceFor(remove))
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            var removed = Remove(remove, 0);
            if (removed <= 0) return false;
            var add = _itemBuilder.Create().WithId(995).WithCount(removed).Build();
            if (!_owner.Inventory.Add(add))
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                Add(add);
                return false;
            }

            SendMoneyPouchChangedMessage(-removed);
            return true;
        }

        /// <summary>
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// Returns true if contained; false otherwise.
        /// </returns>
        public override bool Contains(int id, int count) => !base.Contains(id, count) && _owner.Inventory.Contains(id, count);

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null)
        {
            if (_previousCount != Count) _owner.EventManager.SendEvent(new MoneyPouchChangedEvent(_owner, _previousCount, Count));
        }

        public void Hydrate(IReadOnlyList<HydratedItemDto> moneyPouch)
        {
            var items = new IItem[Capacity];
            foreach (var hydrated in moneyPouch)
            {
                var builder = _itemBuilder.Create().WithId(hydrated.ItemId).WithCount(hydrated.Count);
                if (!string.IsNullOrEmpty(hydrated.ExtraData))
                {
                    builder.WithExtraData(hydrated.ExtraData);
                }
                items[hydrated.SlotId] = builder.Build();
            }
            SetItems(items, false);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            var items = ToArray();
            return items
                .OfType<IItem>()
                .Select((item, index) => new HydratedItemDto(item.Id, item.Count, index, item.SerializeExtraData()))
                .ToList();
        }
    }
}