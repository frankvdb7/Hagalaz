namespace Raido.Server.Tests
{
    [TestClass]
    public class RaidoHubTests
    {
        private class TestRaidoHub : RaidoHub
        {
            public bool DisposedCalled { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposedCalled = true;
                base.Dispose(disposing);
            }
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
    }
}
