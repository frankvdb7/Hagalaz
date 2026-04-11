using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.GameObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.GameObjects
{
    [TestClass]
    public class CabbageTests
    {
        private IRsTaskService _rsTaskService = null!;
        private IItemBuilder _itemBuilder = null!;
        private IGameObject _gameObject = null!;
        private ICharacter _character = null!;
        private Cabbage _cabbage = null!;

        [TestInitialize]
        public void Initialize()
        {
            _rsTaskService = Substitute.For<IRsTaskService>();
            _itemBuilder = Substitute.For<IItemBuilder>();
            _gameObject = Substitute.For<IGameObject>();
            _character = Substitute.For<ICharacter>();

            var location = Substitute.For<ILocation>();
            location.RegionId.Returns(123);
            location.Dimension.Returns(0);
            _gameObject.Location.Returns(location);

            _cabbage = new Cabbage(_rsTaskService, _itemBuilder);
            _cabbage.Initialize(_gameObject);
        }

        [TestMethod]
        public void OnCharacterClickPerform_Option2Click_PullsCabbage()
        {
            var itemMock = Substitute.For<IItem>();
            var itemIdMock = Substitute.For<IItemId>();
            var itemOptionalMock = Substitute.For<IItemOptional>();

            _itemBuilder.Create().Returns(itemIdMock);
            itemIdMock.WithId(Arg.Any<int>()).Returns(itemOptionalMock);
            itemOptionalMock.Build().Returns(itemMock);

            _character.Inventory.Add(itemMock).Returns(true);

            var regionMock = Substitute.For<IMapRegion>();
            var mapRegionServiceMock = Substitute.For<IMapRegionService>();
            mapRegionServiceMock.GetOrCreateMapRegion(123, 0, false).Returns(regionMock);
            _character.ServiceProvider.GetService(typeof(IMapRegionService)).Returns(mapRegionServiceMock);

            _cabbage.OnCharacterClickPerform(_character, GameObjectClickType.Option2Click);

            _character.Received(1).Interrupt(_cabbage);
            _character.Received(1).QueueTask(Arg.Any<ITaskItem>());
        }
    }
}
