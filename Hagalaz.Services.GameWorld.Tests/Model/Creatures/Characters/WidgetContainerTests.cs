using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Services.GameWorld.Model.Widgets;

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

            _widgetScriptProviderMock = Substitute.For<IWidgetScriptProvider>();

            _widgetContainer = new WidgetContainer(_characterMock, _widgetScriptProviderMock);
            _characterMock.Widgets.Returns(_widgetContainer);
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
        public void CloseAll_WhenMultipleNestedWidgetsAreOpen_ClosesEachWidgetOnce()
        {
            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);
            var frameScript = Substitute.For<IWidgetScript>();
            var frame = OpenFrame(1, frameScript);
            var parentScript = Substitute.For<IWidgetScript>();
            var parent = OpenWidget(2, frame, 0, parentScript);
            var childScript = Substitute.For<IWidgetScript>();
            OpenWidget(3, parent, 0, childScript);
            var siblingScript = Substitute.For<IWidgetScript>();
            OpenWidget(4, frame, 1, siblingScript);

            _widgetContainer.CloseAll();

            Assert.AreEqual(0, _widgetContainer.Widgets.Count);
            Assert.IsNull(_widgetContainer.CurrentFrame);
            frameScript.Received(1).OnClose();
            parentScript.Received(1).OnClose();
            childScript.Received(1).OnClose();
            siblingScript.Received(1).OnClose();
        }

        [TestMethod]
        public void OpenFrame_WhenCloseCallbackOpensAnotherFrame_DoesNotReplaceThatFrame()
        {
            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);
            var script = Substitute.For<IWidgetScript>();
            script.When(value => value.OnClose()).Do(_ => _widgetContainer.OpenFrame(CreateFrame(3)));
            var initialFrame = OpenFrame(1, script);
            var requestedFrame = CreateFrame(2);

            _widgetContainer.OpenFrame(requestedFrame);

            Assert.IsFalse(initialFrame.IsOpened);
            Assert.AreEqual(3, _widgetContainer.CurrentFrame!.Id);
            Assert.IsFalse(requestedFrame.IsOpened);
            Assert.AreEqual(1, _widgetContainer.Widgets.Count);
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

        [TestMethod]
        public void WidgetBuilder_BuildNonFrameWithoutActiveFrame_ThrowsInvalidOperationException()
        {
            // Arrange
            var scriptActivatorMock = Substitute.For<IWidgetScriptActivator>();
            var builder = new WidgetBuilder(_widgetScriptProviderMock, scriptActivatorMock);
            _widgetScriptProviderMock.FindScriptTypeById(100).Returns(typeof(IWidgetScript));

            var scriptMock = Substitute.For<IWidgetScript>();
            scriptActivatorMock.Create(_characterMock, typeof(IWidgetScript)).Returns(scriptMock);

            // Act & Assert
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                builder.ForCharacter(_characterMock)
                       .WithId(100)
                       .Build()
            );
            StringAssert.Contains(ex.Message, "no game frame is currently open");
        }

        [TestMethod]
        public void WidgetBuilder_BuildAsFrameWithoutActiveFrame_Succeeds()
        {
            // Arrange
            var scriptActivatorMock = Substitute.For<IWidgetScriptActivator>();
            var builder = new WidgetBuilder(_widgetScriptProviderMock, scriptActivatorMock);
            _widgetScriptProviderMock.FindScriptTypeById(100).Returns(typeof(IWidgetScript));

            var scriptMock = Substitute.For<IWidgetScript>();
            scriptActivatorMock.Create(_characterMock, typeof(IWidgetScript)).Returns(scriptMock);

            // Act
            var widget = builder.ForCharacter(_characterMock)
                                .WithId(100)
                                .AsFrame()
                                .Build();

            // Assert
            Assert.IsNotNull(widget);
            Assert.IsTrue(widget.IsFrame);
        }

        private IWidget OpenFrame(int id, IWidgetScript script)
        {
            var frame = CreateFrame(id, script);
            _widgetContainer.OpenFrame(frame);
            return frame;
        }

        private IWidget OpenWidget(int id, IWidget parent, int slot, IWidgetScript script)
        {
            var widget = new Widget(_characterMock, id, parent.Id, slot, 0, script);
            _widgetContainer.OpenWidget(widget);
            return widget;
        }

        private IWidget CreateFrame(int id, IWidgetScript? script = null)
        {
            return new Widget(_characterMock, id, 0, script ?? Substitute.For<IWidgetScript>());
        }

    }
}
