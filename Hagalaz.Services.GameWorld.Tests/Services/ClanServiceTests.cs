using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Services.GameWorld.Services;
using System.Collections.Generic;
using System;

namespace Hagalaz.Services.GameWorld.Tests.Services
{
    [TestClass]
    public class ClanServiceTests
    {
        private ClanService _clanService;

        [TestInitialize]
        public void Setup()
        {
            _clanService = new ClanService();
        }

        [TestMethod]
        public void PutClan_SubscribesToOnChanged()
        {
            // Arrange
            var clan = Substitute.For<IClan>();
            var settings = Substitute.For<IClanSettings>();
            clan.Name.Returns("TestClan");
            clan.Settings.Returns(settings);

            // Act
            _clanService.PutClan(clan);

            // Assert
            settings.Received(1).OnChanged += Arg.Any<Action>();
        }

        [TestMethod]
        public void RemoveClan_UnsubscribesFromOnChanged()
        {
            // Arrange
            var clan = Substitute.For<IClan>();
            var settings = Substitute.For<IClanSettings>();
            clan.Name.Returns("TestClan");
            clan.Settings.Returns(settings);

            _clanService.PutClan(clan);

            // Act
            _clanService.RemoveClan("TestClan");

            // Assert
            settings.Received(1).OnChanged -= Arg.Any<Action>();
        }

        [TestMethod]
        public void MultipleClans_HandlersDoNotInterfere()
        {
            // Arrange
            var clan1 = Substitute.For<IClan>();
            var settings1 = Substitute.For<IClanSettings>();
            clan1.Name.Returns("Clan1");
            clan1.Settings.Returns(settings1);

            var clan2 = Substitute.For<IClan>();
            var settings2 = Substitute.For<IClanSettings>();
            clan2.Name.Returns("Clan2");
            clan2.Settings.Returns(settings2);

            // Act
            _clanService.PutClan(clan1);
            _clanService.PutClan(clan2);

            // Assert
            settings1.Received(1).OnChanged += Arg.Any<Action>();
            settings2.Received(1).OnChanged += Arg.Any<Action>();

            // Act & Assert Removal
            _clanService.RemoveClan("Clan1");
            settings1.Received(1).OnChanged -= Arg.Any<Action>();
            settings2.DidNotReceive().OnChanged -= Arg.Any<Action>();
        }

        [TestMethod]
        public void PutClan_ReplacesExistingClanAndUpdatesHandlers()
        {
            // Arrange
            var clan1 = Substitute.For<IClan>();
            var settings1 = Substitute.For<IClanSettings>();
            clan1.Name.Returns("TestClan");
            clan1.Settings.Returns(settings1);

            var clan2 = Substitute.For<IClan>();
            var settings2 = Substitute.For<IClanSettings>();
            clan2.Name.Returns("TestClan");
            clan2.Settings.Returns(settings2);

            // Act
            _clanService.PutClan(clan1);
            _clanService.PutClan(clan2);

            // Assert
            settings1.Received(1).OnChanged -= Arg.Any<Action>(); // Should have been unsubscribed when clan1 was replaced
            settings2.Received(1).OnChanged += Arg.Any<Action>();
        }
    }
}
