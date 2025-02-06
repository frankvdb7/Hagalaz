using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Logic.Shops;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    /// <summary>
    /// Class Shop
    /// </summary>
    public class Shop : IShop, ITaskItem
    {
        /// <summary>
        /// 
        /// </summary>
        private int _tickCount;

        /// <summary>
        /// The name of the shop.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the currency identifier.
        /// </summary>
        /// <value>
        /// The currency identifier.
        /// </value>
        public int CurrencyId { get; }

        /// <summary>
        /// The shop container that holds the main stock.
        /// </summary>
        /// <value>The main stock container.</value>
        public IShopStockContainer MainStockContainer { get; }

        /// <summary>
        /// The shop container that holds the main stock.
        /// </summary>
        /// <value>The sample stock container.</value>
        public IShopStockContainer SampleStockContainer { get; }

        /// <summary>
        /// Gets a value indicating whether [general store].
        /// </summary>
        /// <value><c>true</c> if [general store]; otherwise, <c>false</c>.</value>
        public bool GeneralStore { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shop" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="currencyId">The currency identifier.</param>
        /// <param name="mainStock"></param>
        /// <param name="sampleStock">if set to <c>true</c> [sample stock].</param>
        /// <param name="generalStore">if set to <c>true</c> [general store].</param>
        /// <param name="itemRepository"></param>
        /// <param name="itemBuilder"></param>
        public Shop(
            string name, int capacity, int currencyId, bool generalStore, IEnumerable<IItem> mainStock, IEnumerable<IItem> sampleStock,
            IItemService itemRepository, IItemBuilder itemBuilder)
        {
            Name = name;
            CurrencyId = currencyId;
            MainStockContainer = new ShopStockContainer(this, itemRepository, itemBuilder, false, StorageType.AlwaysStack, capacity, mainStock.ToList());
            SampleStockContainer = new ShopStockContainer(this, itemRepository, itemBuilder, true, StorageType.AlwaysStack, capacity, sampleStock.ToList());
            GeneralStore = generalStore;
        }

        /// <summary>
        /// Refreshes the price.
        /// </summary>
        /// <param name="viewer">The viewer.</param>
        /// <param name="changedSlots">The changed slots.</param>
        public void RefreshPrice(ICharacter viewer, HashSet<int>? changedSlots)
        {
            if (changedSlots == null || changedSlots.Count == MainStockContainer.Capacity)
            {
                for (var slot = 0; slot < MainStockContainer.Capacity; slot++)
                {
                    var item = MainStockContainer[slot];
                    if (item != null) viewer.Configurations.SendGlobalCs2Int(946 + slot, 0); // -1 is N/A and 0 is owner price.
                }
            }
            else
            {
                foreach (var slot in changedSlots)
                {
                    var item = MainStockContainer[slot];
                    if (item != null) viewer.Configurations.SendGlobalCs2Int(946 + slot, 0); // -1 is N/A and 0 is owner price.
                }
            }
        }

        public int GetBuyValue(IItem item) => item.ItemDefinition.Value;

        public int GetSellValue(IItem item)
        {
            var value = item.ItemDefinition.Value;
            const double modifier = 0.9;
            var sellValue = (int)(value * modifier);
            return sellValue <= 0 ? 1 : sellValue;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Tick()
        {
            // Perform every 60 seconds (100 * 600 ms)
            if (_tickCount++ % 100 != 0)
            {
                return;
            }

            MainStockContainer.NormalizeStock();
            SampleStockContainer.NormalizeStock();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCancelled { get; } = false;
        public bool IsCompleted { get; } = false;
        public bool IsFaulted { get; } = false;
        public void Cancel() {}
    }
}