using System;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Services.GameWorld.Services;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameWorld.Tests.Services
{
    [TestClass]
    public class ClanServiceTests
    {
        [TestMethod]
        public void GetClanByName_WhenClanDoesNotExist_ReturnsNull()
        {
            // Arrange
            var service = new ClanService();

            // Act
            var result = service.GetClanByName("NonExistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void PutClan_AddsClanCorrectly()
        {
            // Arrange
            var service = new ClanService();
            var clan = Substitute.For<IClan>();
            clan.Name.Returns("TestClan");

            // Act
            service.PutClan(clan);

            // Assert
            Assert.AreEqual(clan, service.GetClanByName("TestClan"));
        }
    }
}
