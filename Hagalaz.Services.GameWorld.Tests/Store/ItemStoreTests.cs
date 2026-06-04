using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Hagalaz.Services.GameWorld.Tests.Store
{
    [TestClass]
    public class ItemStoreTests
    {
        private IServiceProvider _serviceProviderMock;
        private ITypeProvider<IItemDefinition> _itemProviderMock;
        private ILogger<ItemStore> _loggerMock;
        private ItemStore _itemStore;

        [TestInitialize]
        public void Setup()
        {
            _serviceProviderMock = Substitute.For<IServiceProvider>();
            _itemProviderMock = Substitute.For<ITypeProvider<IItemDefinition>>();
            _loggerMock = Substitute.For<ILogger<ItemStore>>();

            _itemStore = new ItemStore(_serviceProviderMock, _itemProviderMock, _loggerMock);
        }

        [TestMethod]
        public void GetOrAdd_ProviderReturnsNullButInDatabase_ReturnsNullAndDoesNotThrow()
        {
            // Arrange
            int itemId = 123;
            var dbItem = new Hagalaz.Data.Entities.ItemDefinition { Id = (ushort)itemId, Examine = "Test" };

            // Use reflection to populate the private _databaseItems field
            var databaseItemsField = typeof(ItemStore).GetField("_databaseItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var databaseItems = new Dictionary<int, Hagalaz.Data.Entities.ItemDefinition>
            {
                { itemId, dbItem }
            };
            databaseItemsField!.SetValue(_itemStore, databaseItems);

            // Mock provider to return null
            _itemProviderMock.Get(itemId).Returns((IItemDefinition)null!);

            // Act
            var result = _itemStore.GetOrAdd(itemId);

            // Assert
            Assert.IsNull(result);
        }
    }
}
