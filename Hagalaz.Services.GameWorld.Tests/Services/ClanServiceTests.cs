using Hagalaz.Services.GameWorld.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameWorld.Tests.Services
{
    [TestClass]
    public class ClanServiceTests
    {
        [TestMethod]
        public void GetClanByName_WhenNotFound_ReturnsNull()
        {
            var service = new ClanService();
            var clan = service.GetClanByName("NonExistent");
            Assert.IsNull(clan);
        }
    }
}
