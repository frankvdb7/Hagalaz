namespace Raido.Server.Tests
{
    [TestClass]
    public class RaidoHubTests
    {
        private class TestRaidoHub : RaidoHub
        {
            public bool OnConnectedAsyncCalled { get; private set; }
            public bool OnDisconnectedAsyncCalled { get; private set; }
            public bool DisposedCalled { get; private set; }

            public override Task OnConnectedAsync()
            {
                OnConnectedAsyncCalled = true;
                return base.OnConnectedAsync();
            }

            public override Task OnDisconnectedAsync(Exception? exception)
            {
                OnDisconnectedAsyncCalled = true;
                return base.OnDisconnectedAsync(exception);
            }

            protected override void Dispose(bool disposing)
            {
                DisposedCalled = true;
                base.Dispose(disposing);
            }
        }

        [TestMethod]
        public async Task OnConnectedAsync_ShouldBeCalled()
        {
            // Arrange
            var hub = new TestRaidoHub();

            // Act
            await hub.OnConnectedAsync();

            // Assert
            Assert.IsTrue(hub.OnConnectedAsyncCalled);
        }

        [TestMethod]
        public async Task OnDisconnectedAsync_ShouldBeCalled()
        {
            // Arrange
            var hub = new TestRaidoHub();

            // Act
            await hub.OnDisconnectedAsync(null);

            // Assert
            Assert.IsTrue(hub.OnDisconnectedAsyncCalled);
        }

        [TestMethod]
        public void Dispose_ShouldCallDisposeAndSetDisposedFlag()
        {
            // Arrange
            var hub = new TestRaidoHub();

            // Act
            hub.Dispose();

            // Assert
            Assert.IsTrue(hub.DisposedCalled);
            Assert.ThrowsException<ObjectDisposedException>(() => hub.Clients);
            Assert.ThrowsException<ObjectDisposedException>(() => hub.Context);
        }

        [TestMethod]
        public void Clients_ShouldThrowWhenDisposed()
        {
            // Arrange
            var hub = new TestRaidoHub();
            hub.Dispose();

            // Act & Assert
            Assert.ThrowsException<ObjectDisposedException>(() => hub.Clients);
        }

        [TestMethod]
        public void Context_ShouldThrowWhenDisposed()
        {
            // Arrange
            var hub = new TestRaidoHub();
            hub.Dispose();

            // Act & Assert
            Assert.ThrowsException<ObjectDisposedException>(() => hub.Context);
        }
    }
}
