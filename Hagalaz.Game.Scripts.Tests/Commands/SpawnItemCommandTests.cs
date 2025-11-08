using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SpawnItemCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SpawnsItem()
        {
            // Arrange
            var itemBuilderMock = Substitute.For<IItemBuilder>();
            var itemIdMock = Substitute.For<IItemId>();
            var itemOptionalMock = Substitute.For<IItemOptional>();
            var itemBuildMock = Substitute.For<IItemBuild>();
            var itemMock = Substitute.For<IItem>();

            itemBuilderMock.Create().Returns(itemIdMock);
            itemIdMock.WithId(123).Returns(itemOptionalMock);
            itemOptionalMock.WithCount(456).Returns(itemOptionalMock);
            ((IItemBuild)itemOptionalMock).Build().Returns(itemMock);

            var groundItemBuilderMock = Substitute.For<IGroundItemBuilder>();
            var groundItemOnGroundMock = Substitute.For<IGroundItemOnGround>();
            var groundItemLocationMock = Substitute.For<IGroundItemLocation>();
            var groundItemOptionalMock = Substitute.For<IGroundItemOptional>();
            var groundItemBuildMock = Substitute.For<IGroundItemBuild>();

            groundItemBuilderMock.Create().Returns(groundItemOnGroundMock);
            groundItemOnGroundMock.WithItem(Arg.Any<Func<IItemBuilder, IItemBuild>>()).Returns(groundItemLocationMock);
            groundItemLocationMock.WithLocation(Arg.Any<Location>()).Returns(groundItemOptionalMock);
            groundItemOptionalMock.WithTicks(Arg.Any<int>()).Returns(groundItemOptionalMock);
            ((IGroundItemBuild)groundItemOptionalMock).Spawn();

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IGroundItemBuilder)).Returns(groundItemBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Location.Returns(new Location(0,0,0,0));

            var command = new SpawnItemCommand();
            var args = new GameCommandArgs(characterMock, new[] { "spawnitem", "123", "456", "789" });

            // Act
            await command.Execute(args);

            // Assert
            ((IGroundItemBuild)groundItemOptionalMock).Received(1).Spawn();
        }
    }
}
