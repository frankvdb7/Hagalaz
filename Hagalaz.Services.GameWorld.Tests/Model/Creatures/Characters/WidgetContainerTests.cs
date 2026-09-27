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
        public void CloseWidget_WhenChildCloseGuardRejects_LeavesWidgetTreeAttachedAndOpen()
        {
            var (frame, widget, script) = OpenGuardedWidgetTree();

            _widgetContainer.CloseWidget(frame);

            Assert.AreSame(frame, _widgetContainer.CurrentFrame);
            Assert.IsTrue(frame.IsOpened);
            Assert.IsTrue(widget.IsOpened);
            Assert.AreSame(widget, frame.GetChild(0));
            Assert.AreSame(widget, _widgetContainer.GetOpenWidget(widget.Id));
            ((IWidgetCloseGuard)script).Received(1).TryClose();
        }

        [TestMethod]
        public void CloseWidget_WhenGuardRejects_RedrawsOpenWidgetWithoutRepeatingLifecycle()
        {
            var (frame, widget, script) = OpenGuardedWidgetTree();
            _sessionMock.ClearReceivedCalls();

            _widgetContainer.CloseWidget(widget);

            _sessionMock.Received(1).SendMessage(Arg.Is<DrawInterfaceComponentMessage>(message =>
                message.Id == widget.Id && message.ParentId == frame.Id && message.ParentSlot == widget.ParentSlot && message.Transparency == widget.Transparency));
            Assert.AreSame(widget, frame.GetChild(widget.ParentSlot));
            Assert.AreSame(widget, _widgetContainer.GetOpenWidget(widget.Id));
            Assert.AreEqual(2, _widgetContainer.Widgets.Count);
            Assert.IsTrue(widget.IsOpened);
            script.Received(1).OnOpen();
            script.DidNotReceive().OnClose();
            ((IWidgetCloseGuard)script).Received(1).TryClose();
        }

        [TestMethod]
        public void CloseAll_WhenWidgetTreeIsOpen_ClosesEachWidgetOnce()
        {
            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);
            var frameScript = Substitute.For<IWidgetScript>();
            var frame = OpenFrame(1, frameScript);
            var parentScript = Substitute.For<IWidgetScript>();
            var parent = OpenWidget(2, frame, 0, parentScript);
            var childScript = Substitute.For<IWidgetScript>();
            OpenWidget(3, parent, 0, childScript);

            var closed = _widgetContainer.CloseAll();

            Assert.IsTrue(closed);
            Assert.AreEqual(0, _widgetContainer.Widgets.Count);
            Assert.IsNull(_widgetContainer.CurrentFrame);
            frameScript.Received(1).OnClose();
            parentScript.Received(1).OnClose();
            childScript.Received(1).OnClose();
        }

        [TestMethod]
        public void CloseAll_WhenChildGuardRejects_LeavesWholeTreeOpenWithoutLifecycleCallbacks()
        {
            var (frame, guardedChild, guardedScript) = OpenGuardedWidgetTree();
            var frameScript = frame.Script;
            var siblingScript = Substitute.For<IWidgetScript>();
            OpenWidget(3, frame, 1, siblingScript);

            var closed = _widgetContainer.CloseAll();

            Assert.IsFalse(closed);
            Assert.AreSame(frame, _widgetContainer.CurrentFrame);
            Assert.IsTrue(frame.IsOpened);
            Assert.IsTrue(guardedChild.IsOpened);
            Assert.AreSame(guardedChild, frame.GetChild(guardedChild.ParentSlot));
            Assert.IsTrue(_widgetContainer.Widgets.Contains(guardedChild));
            Assert.IsTrue(_widgetContainer.Widgets.Any(widget => widget.Script == siblingScript));
            ((IWidgetCloseGuard)guardedScript).Received(1).TryClose();
            frameScript.DidNotReceive().OnClose();
            siblingScript.DidNotReceive().OnClose();
            guardedScript.DidNotReceive().OnClose();
        }

        [TestMethod]
        public void OpenFrame_WhenExistingTreeGuardRejects_KeepsOldFrameAndDoesNotOpenReplacement()
        {
            var (oldFrame, child, guardedScript) = OpenGuardedWidgetTree();
            var replacement = CreateFrame(3);

            _widgetContainer.OpenFrame(replacement);

            Assert.AreSame(oldFrame, _widgetContainer.CurrentFrame);
            Assert.IsTrue(oldFrame.IsOpened);
            Assert.IsFalse(replacement.IsOpened);
            Assert.AreSame(child, oldFrame.GetChild(child.ParentSlot));
            ((IWidgetCloseGuard)guardedScript).Received(1).TryClose();
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

        private (IWidget Frame, IWidget Widget, IWidgetScript Script) OpenGuardedWidgetTree()
        {
            _widgetScriptProviderMock.GetInterfacesCount().Returns(10);
            var frame = OpenFrame(1, Substitute.For<IWidgetScript>());
            var script = Substitute.For<IWidgetScript, IWidgetCloseGuard>();
            ((IWidgetCloseGuard)script).TryClose().Returns(false);
            var widget = OpenWidget(2, frame, 0, script);
            return (frame, widget, script);
        }
    }
}
