using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public class CreatureCombatTests
{
    private Mock<ICharacter> _mockOwner = null!;
    private Mock<ICreature> _mockAttacker = null!;
    private Mock<ICreatureCombat> _mockAttackerCombat = null!;
    private Mock<IViewport> _mockViewport = null!;
    private TestableCharacterCombat _characterCombat = null!;
    private List<ICreature> _visibleCreatures = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockOwner = new Mock<ICharacter>();
        _mockAttacker = new Mock<ICreature>();
        _mockAttackerCombat = new Mock<ICreatureCombat>();
        _mockViewport = new Mock<IViewport>();
        _visibleCreatures = new List<ICreature>();

        _mockAttacker.Setup(a => a.Combat).Returns(_mockAttackerCombat.Object);
        _mockOwner.Setup(o => o.Viewport).Returns(_mockViewport.Object);
        _mockViewport.Setup(v => v.VisibleCreatures).Returns(_visibleCreatures);

        var serviceProviderMock = new Mock<System.IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAnimationBuilder))).Returns(new Mock<IAnimationBuilder>().Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IGraphicBuilder))).Returns(new Mock<IGraphicBuilder>().Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IProjectileBuilder))).Returns(new Mock<IProjectileBuilder>().Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IProjectilePathFinder))).Returns(new Mock<IProjectilePathFinder>().Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ISmartPathFinder))).Returns(new Mock<ISmartPathFinder>().Object);

        var combatOptions = new CombatOptions();
        var optionsMock = new Mock<IOptions<CombatOptions>>();
        optionsMock.Setup(o => o.Value).Returns(combatOptions);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOptions<CombatOptions>))).Returns(optionsMock.Object);

        _mockOwner.Setup(o => o.ServiceProvider).Returns(serviceProviderMock.Object);

        _characterCombat = new TestableCharacterCombat(_mockOwner.Object);
    }

    [TestMethod]
    public void CanBeAttackedBy_OwnerIsDead_ReturnsFalse()
    {
        // Arrange
        _characterCombat.SetDead(true);

        // Act
        var result = _characterCombat.CanBeAttackedBy(_mockAttacker.Object);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CanBeAttackedBy_AttackerIsDead_ReturnsFalse()
    {
        // Arrange
        _mockAttackerCombat.Setup(ac => ac.IsDead).Returns(true);

        // Act
        var result = _characterCombat.CanBeAttackedBy(_mockAttacker.Object);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CanBeAttackedBy_AttackerIsDestroyed_ReturnsFalse()
    {
        // Arrange
        _mockAttacker.Setup(a => a.IsDestroyed).Returns(true);

        // Act
        var result = _characterCombat.CanBeAttackedBy(_mockAttacker.Object);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CanBeAttackedBy_AttackerNotInViewport_ReturnsFalse()
    {
        // Arrange

        // Act
        var result = _characterCombat.CanBeAttackedBy(_mockAttacker.Object);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CanBeAttackedBy_ScriptReturnsFalse_ReturnsFalse()
    {
        // Arrange
        _visibleCreatures.Add(_mockAttacker.Object);
        var mockScript = new Mock<ICharacterScript>();
        mockScript.Setup(s => s.CanBeAttackedBy(_mockAttacker.Object)).Returns(false);
        _mockOwner.Setup(o => o.GetScripts()).Returns(new[] { mockScript.Object });

        // Act
        var result = _characterCombat.CanBeAttackedBy(_mockAttacker.Object);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CanBeAttackedBy_AllConditionsMet_ReturnsTrue()
    {
        // Arrange
        _visibleCreatures.Add(_mockAttacker.Object);
        _mockOwner.Setup(o => o.GetScripts()).Returns(new List<ICharacterScript>());

        // Act
        var result = _characterCombat.CanBeAttackedBy(_mockAttacker.Object);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsInCombat_HasTarget_ReturnsTrue()
    {
        // Arrange
        var mockTarget = new Mock<ICreature>();
        _characterCombat.SetTargetForTest(mockTarget.Object);

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
        _characterCombat.AddAttackerPublic(_mockAttacker.Object);

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