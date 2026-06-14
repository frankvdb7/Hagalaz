using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

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
        public void GetOrAdd_MissingDefinition_ReturnsEmptyDefinitionWithNullName()
        {
            // Arrange
            int itemId = 1;
            _itemProviderMock.Get(itemId).Returns((IItemDefinition)null);

            // Act
            var result = _itemStore.GetOrAdd(itemId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("null", result.Name);
        }
    }
}
