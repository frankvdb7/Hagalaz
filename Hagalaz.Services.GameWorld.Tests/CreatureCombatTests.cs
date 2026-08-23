using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CreatureCombatTests
    {
        private ICharacter _mockOwner = null!;
        private ICreature _mockAttacker = null!;
        private ICreatureCombat _mockAttackerCombat = null!;
        private IViewport _mockViewport = null!;
        private TestableCharacterCombat _characterCombat = null!;
        private List<ICreature> _visibleCreatures = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockOwner = Substitute.For<ICharacter>();
            _mockAttacker = Substitute.For<ICreature>();
            _mockAttackerCombat = Substitute.For<ICreatureCombat>();
            _mockViewport = Substitute.For<IViewport>();
            _visibleCreatures = new List<ICreature>();

            _mockAttacker.Combat.Returns(_mockAttackerCombat);
            _mockOwner.Viewport.Returns(_mockViewport);
            _mockViewport.VisibleCreatures.Returns(_visibleCreatures);

            _characterCombat = new TestableCharacterCombat(
                _mockOwner,
                Substitute.For<IAnimationBuilder>(),
                Substitute.For<IGraphicBuilder>(),
                Substitute.For<IProjectileBuilder>(),
                Substitute.For<IMapRegionService>(),
                Substitute.For<IGroundItemBuilder>(),
                Substitute.For<IHitSplatBuilder>(),
                Substitute.For<IProjectilePathFinder>(),
                Substitute.For<ISmartPathFinder>(),
                Options.Create(new CombatOptions()));
        }

        [TestMethod]
        public void CanBeAttackedBy_OwnerIsDead_ReturnsFalse()
        {
            // Arrange
            _characterCombat.SetDead(true);

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AttackerIsDead_ReturnsFalse()
        {
            // Arrange
            _mockAttackerCombat.IsDead.Returns(true);

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AttackerIsDestroyed_ReturnsFalse()
        {
            // Arrange
            _mockAttacker.IsDestroyed.Returns(true);

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AttackerNotInViewport_ReturnsFalse()
        {
            // Arrange

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_ScriptReturnsFalse_ReturnsFalse()
        {
            // Arrange
            _visibleCreatures.Add(_mockAttacker);
            var mockScript = Substitute.For<ICharacterScript>();
            mockScript.CanBeAttackedBy(_mockAttacker).Returns(false);
            _mockOwner.GetScripts().Returns(new[] { mockScript });

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AllConditionsMet_ReturnsTrue()
        {
            // Arrange
            _visibleCreatures.Add(_mockAttacker);
            _mockOwner.GetScripts().Returns(new List<ICharacterScript>());

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInCombat_HasTarget_ReturnsTrue()
        {
            // Arrange
            var mockTarget = Substitute.For<ICreature>();
            _characterCombat.SetTargetForTest(mockTarget);

            // Act
            var result = _characterCombat.IsInCombat();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInCombat_HasRecentAttackers_ReturnsTrue()
        {
            // Arrange
            _characterCombat.SetTargetForTest(null);
            _characterCombat.AddAttackerPublic(_mockAttacker);

            // Act
            var result = _characterCombat.IsInCombat();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInCombat_NoTargetAndNoRecentAttackers_ReturnsFalse()
        {
            // Arrange
            _characterCombat.SetTargetForTest(null);

            // Act
            var result = _characterCombat.IsInCombat();

            // Assert
            Assert.IsFalse(result);
        }
    }

    public class TestableCharacterCombat : CharacterCombat
    {
        public TestableCharacterCombat(
            ICharacter owner,
            IAnimationBuilder animationBuilder,
            IGraphicBuilder graphicBuilder,
            IProjectileBuilder projectileBuilder,
            IMapRegionService mapRegionService,
            IGroundItemBuilder groundItemBuilder,
            IHitSplatBuilder hitSplatBuilder,
            IProjectilePathFinder projectilePathFinder,
            ISmartPathFinder smartPathFinder,
            IOptions<CombatOptions> combatOptions)
            : base(owner, animationBuilder, graphicBuilder, projectileBuilder, mapRegionService, groundItemBuilder,
                hitSplatBuilder, projectilePathFinder, smartPathFinder, combatOptions)
        {
        }

        public void SetDead(bool isDead)
        {
            IsDead = isDead;
        }

        public void SetTargetForTest(ICreature? target)
        {
            Target = target;
        }

        public void AddAttackerPublic(ICreature attacker)
        {
            AddAttacker(attacker);
        }
    }
}
