using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Services.GameWorld.Data;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class InMemoryEventBusTests
    {
        private InMemoryEventBus _eventBus;

        [TestInitialize]
        public void Setup()
        {
            _eventBus = new InMemoryEventBus();
        }

        [TestMethod]
        public void SendEvent_WithNoSubscribers_ReturnsTrue()
        {
            var testEvent = new TestEvent();
            var result = _eventBus.SendEvent(testEvent);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SendEvent_WithSubscriber_InvokesHandler()
        {
            var handler = Substitute.For<EventHappened<TestEvent>>();
            _eventBus.Listen(handler);
            var testEvent = new TestEvent();

            _eventBus.SendEvent(testEvent);

            handler.Received(1).Invoke(testEvent);
        }

        [TestMethod]
        public void SendEvent_WithMultipleSubscribers_InvokesAllHandlers()
        {
            var handler1 = Substitute.For<EventHappened<TestEvent>>();
            var handler2 = Substitute.For<EventHappened<TestEvent>>();
            _eventBus.Listen(handler1);
            _eventBus.Listen(handler2);
            var testEvent = new TestEvent();

            _eventBus.SendEvent(testEvent);

            handler1.Received(1).Invoke(testEvent);
            handler2.Received(1).Invoke(testEvent);
        }

        [TestMethod]
        public void StopListen_RemovesHandler()
        {
            var handler = Substitute.For<EventHappened<TestEvent>>();
            var handlerToRemove = _eventBus.Listen(handler);
            _eventBus.StopListen<TestEvent>(handlerToRemove);
            var testEvent = new TestEvent();

            _eventBus.SendEvent(testEvent);

            handler.DidNotReceive().Invoke(testEvent);
        }

        private class TestEvent : IEvent
        {
        }
    }
}
