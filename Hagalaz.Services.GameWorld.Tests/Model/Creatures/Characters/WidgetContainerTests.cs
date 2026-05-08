using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Abstractions.Data;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Characters
{
    [TestClass]
    public class WidgetContainerTests
    {
        private ICharacter _characterMock;
        private IGameSession _sessionMock;
        private IWidgetScriptProvider _widgetScriptProviderMock;
        private IEventManager _eventManagerMock;
        private WidgetContainer _widgetContainer;

        [TestInitialize]
        public void Setup()
        {
            _characterMock = Substitute.For<ICharacter>();
            _sessionMock = Substitute.For<IGameSession>();
            _characterMock.Session.Returns(_sessionMock);

            _eventManagerMock = Substitute.For<IEventManager>();
            _characterMock.EventManager.Returns(_eventManagerMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            _characterMock.ServiceProvider.Returns(serviceProviderMock);

            _widgetScriptProviderMock = Substitute.For<IWidgetScriptProvider>();
            serviceProviderMock.GetService(typeof(IWidgetScriptProvider)).Returns(_widgetScriptProviderMock);

            _widgetContainer = new WidgetContainer(_characterMock);
        }

        [TestMethod]
        public void OpenFrame_FirstFrame_ForceRedrawIsFalseByDefault()
        {
            // Arrange
            var frame = Substitute.For<IWidget>();
            frame.Id.Returns(1);
            frame.IsFrame.Returns(true);
            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);

            // Act
            _widgetContainer.OpenFrame(frame);

            // Assert
            _sessionMock.Received(1).SendMessage(Arg.Is<DrawFrameComponentMessage>(m => m.Id == 1 && m.ForceRedraw == false));
            Assert.AreEqual(frame, _widgetContainer.CurrentFrame);
        }

        [TestMethod]
        public void OpenFrame_SecondFrame_ForceRedrawIsTrue()
        {
            // Arrange
            var frame1 = Substitute.For<IWidget>();
            frame1.Id.Returns(1);
            frame1.IsFrame.Returns(true);

            var frame2 = Substitute.For<IWidget>();
            frame2.Id.Returns(2);
            frame2.IsFrame.Returns(true);

            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);

            _widgetContainer.OpenFrame(frame1);

            // Act
            _widgetContainer.OpenFrame(frame2);

            // Assert
            _sessionMock.Received(1).SendMessage(Arg.Is<DrawFrameComponentMessage>(m => m.Id == 2 && m.ForceRedraw == true));
            Assert.AreEqual(frame2, _widgetContainer.CurrentFrame);
        }

        [TestMethod]
        public void OpenFrame_ExplicitForceRedraw_ForceRedrawIsTrue()
        {
            // Arrange
            var frame = Substitute.For<IWidget>();
            frame.Id.Returns(1);
            frame.IsFrame.Returns(true);
            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);

            // Act
            _widgetContainer.OpenFrame(frame, true);

            // Assert
            _sessionMock.Received(1).SendMessage(Arg.Is<DrawFrameComponentMessage>(m => m.Id == 1 && m.ForceRedraw == true));
        }
    }
}
