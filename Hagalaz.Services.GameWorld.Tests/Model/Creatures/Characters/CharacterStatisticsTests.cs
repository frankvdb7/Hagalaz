using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.Options;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using System;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Characters
{
    [TestClass]
    public class CharacterStatisticsTests
    {
        private ICharacter _characterMock;
        private IServiceProvider _serviceProviderMock;
        private CharacterStatistics _statistics;

        [TestInitialize]
        public void Setup()
        {
            _characterMock = Substitute.For<ICharacter>();
            _serviceProviderMock = Substitute.For<IServiceProvider>();
            _characterMock.ServiceProvider.Returns(_serviceProviderMock);

            var combatOptions = Options.Create(new CombatOptions());
            var skillOptions = Options.Create(new SkillOptions());

            _serviceProviderMock.GetService(typeof(IOptions<CombatOptions>)).Returns(combatOptions);
            _serviceProviderMock.GetService(typeof(IOptions<SkillOptions>)).Returns(skillOptions);

            _statistics = new CharacterStatistics(_characterMock);
        }

        [TestMethod]
        public void Hydrate_WithEmptyTargetArrays_DoesNotThrow()
        {
            // Arrange
            var hydration = new HydratedStatisticsDto
            {
                TargetSkillLevels = Array.Empty<int>(),
                TargetSkillExperiences = Array.Empty<double>(),
                XpCounters = Array.Empty<int>(),
                TrackedXpCounters = Array.Empty<int>(),
                EnabledXpCounters = Array.Empty<bool>()
            };

            // Act & Assert
            try
            {
                _statistics.Hydrate(hydration);
            }
            catch (IndexOutOfRangeException)
            {
                Assert.Fail("Hydrate threw IndexOutOfRangeException with empty arrays.");
            }
        }
    }
}
