using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Hagalaz.Game.Scripts.Model.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SpawnBoxCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SpawnsBox()
        {
            // Arrange
            var itemBuilderMock = Substitute.For<IItemBuilder>();
            var itemServiceMock = Substitute.For<IItemService>();
            var itemDefinitionMock = Substitute.For<IItemDefinition>();
            itemDefinitionMock.Name.Returns("Test Item");
            itemServiceMock.GetTotalItemCount().Returns(1);
            itemServiceMock.FindItemDefinitionById(0).Returns(itemDefinitionMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IItemService)).Returns(itemServiceMock);
            var characterContextAccessor = Substitute.For<Hagalaz.Game.Abstractions.Providers.ICharacterContextAccessor>();
            serviceProviderMock.GetService(typeof(DefaultWidgetScript)).Returns(Substitute.For<DefaultWidgetScript>(characterContextAccessor));

            var configurationsMock = Substitute.For<IConfigurations>();
            var widgetContainerMock = Substitute.For<IWidgetContainer>();

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Configurations.Returns(configurationsMock);
            characterMock.Widgets.Returns(widgetContainerMock);

            var command = new SpawnBoxCommand(itemBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "spawnbox", "test" });

            // Act
            await command.Execute(args);

            // Assert
            widgetContainerMock.Received(1).OpenWidget(645, 0, Arg.Any<DefaultWidgetScript>(), true);
        }
    }
}
