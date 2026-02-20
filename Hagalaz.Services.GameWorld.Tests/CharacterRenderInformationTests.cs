using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CharacterRenderInformationTests
    {
        private ICharacter _owner = null!;
        private IServiceProvider _serviceProvider = null!;
        private ICharacterStore _characterStore = null!;
        private ICharacterLocationService _locationService = null!;
        private CharacterRenderInformation _renderInfo = null!;

        [TestInitialize]
        public void Setup()
        {
            _owner = Substitute.For<ICharacter>();
            _serviceProvider = Substitute.For<IServiceProvider>();
            _characterStore = Substitute.For<ICharacterStore>();
            _locationService = Substitute.For<ICharacterLocationService>();

            _owner.ServiceProvider.Returns(_serviceProvider);
            _serviceProvider.GetService(typeof(ICharacterStore)).Returns(_characterStore);
            _serviceProvider.GetService(typeof(ICharacterLocationService)).Returns(_locationService);

            _renderInfo = new CharacterRenderInformation(_owner);
        }

        [TestMethod]
        public void OnRegistered_InitializesLastLocationAndLocalCharacters()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            _owner.Location.Returns(location);
            location.Clone().Returns(location);

            // Act
            _renderInfo.OnRegistered();

            // Assert
            Assert.AreEqual(location, _renderInfo.LastLocation);
            Assert.IsTrue(_renderInfo.LocalCharacters.Contains(_owner));
            Assert.IsTrue(_renderInfo.IsInViewport(_owner.Index));
        }

        [TestMethod]
        public void Reset_ClearsFlagsAndUpdatesLocation()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            _owner.Location.Returns(location);
            location.Clone().Returns(location);
            _renderInfo.ScheduleFlagUpdate(Hagalaz.Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Animation);
            _renderInfo.ScheduleItemAppearanceUpdate();

            // Act
            _renderInfo.Reset();

            // Assert
            Assert.IsFalse(_renderInfo.FlagUpdateRequired);
            Assert.IsFalse(_renderInfo.ItemAppearanceUpdateRequired);
            _locationService.Received(1).SetLocationByIndex(_owner.Index, location);
        }

        [TestMethod]
        public async Task Update_SendsCorrectMessages()
        {
            // Arrange
            var session = Substitute.For<IGameSession>();
            _owner.Session.Returns(session);

            _characterStore.FindAllAsync().Returns(new List<ICharacter>().ToAsyncEnumerable());

            var viewport = Substitute.For<IViewport>();
            _owner.Viewport.Returns(viewport);
            viewport.VisibleCreatures.Returns(new List<ICreature>());

            // Act
            await _renderInfo.Update();

            // Assert
            session.Received(1).SendMessage(Arg.Any<DrawCharactersMessage>());
            session.Received(1).SendMessage(Arg.Any<DrawNpcsMessage>());
        }

        [TestMethod]
        public void ViewportManagement_WorksCorrectly()
        {
            // Act
            _renderInfo.SetInViewport(5, true);
            _renderInfo.SetJustCrossedViewport(5, true);
            _renderInfo.SetIdle(5, true);
            _renderInfo.SetIdleOnThisLoop(5, true);

            // Assert
            Assert.IsTrue(_renderInfo.IsInViewport(5));
            Assert.IsTrue(_renderInfo.HasJustCrossedViewport(5));
            Assert.IsTrue(_renderInfo.IsIdle(5));
            Assert.IsTrue(_renderInfo.IsIdleOnThisLoop(5));

            // Act reset
            _renderInfo.Reset();

            // Assert crossed is false after reset
            Assert.IsFalse(_renderInfo.HasJustCrossedViewport(5));
            // IdleOnThisLoop is moved to Idle, then cleared
            Assert.IsTrue(_renderInfo.IsIdle(5));
            Assert.IsFalse(_renderInfo.IsIdleOnThisLoop(5));
        }
    }
}
