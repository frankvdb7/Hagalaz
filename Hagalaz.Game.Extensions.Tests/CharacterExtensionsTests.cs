using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class CharacterExtensionsTests
    {
        private ICharacter _character = null!;
        private ICharacterAppearance _appearance = null!;
        private IMovement _movement = null!;
        private IProfile _profile = null!;
        private ISlayer _slayer = null!;

        [TestInitialize]
        public void Setup()
        {
            _character = Substitute.For<ICharacter>();
            _appearance = Substitute.For<ICharacterAppearance>();
            _movement = Substitute.For<IMovement>();
            _profile = Substitute.For<IProfile>();
            _slayer = Substitute.For<ISlayer>();

            _character.Appearance.Returns(_appearance);
            _character.Movement.Returns(_movement);
            _character.Profile.Returns(_profile);
            _character.Slayer.Returns(_slayer);
        }

        [TestMethod]
        public void HasDisplayName_WhenPreviousDisplayNameIsSet_ShouldReturnTrue()
        {
            // Arrange
            _character.PreviousDisplayName.Returns("OldName");

            // Act
            var result = _character.HasDisplayName();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasDisplayName_WhenPreviousDisplayNameIsEmpty_ShouldReturnFalse()
        {
            // Arrange
            _character.PreviousDisplayName.Returns("");

            // Act
            var result = _character.HasDisplayName();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsTransformedToNpc_WhenNpcIdIsSet_ShouldReturnTrue()
        {
            // Arrange
            _appearance.NpcId.Returns(123);

            // Act
            var result = _appearance.IsTransformedToNpc();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTransformedToNpc_WhenNpcIdIsNotSet_ShouldReturnFalse()
        {
            // Arrange
            _appearance.NpcId.Returns(-1);

            // Act
            var result = _appearance.IsTransformedToNpc();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasFamiliar_WhenFamiliarScriptIsNotNull_ReturnsTrue()
        {
            // Arrange
            _character.FamiliarScript.Returns(Substitute.For<IFamiliarScript>());

            // Act
            var result = _character.HasFamiliar();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasFamiliar_WhenFamiliarScriptIsNull_ReturnsFalse()
        {
            // Arrange
            _character.FamiliarScript.Returns((IFamiliarScript)null);

            // Act
            var result = _character.HasFamiliar();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasClan_WhenClanIsNotNull_ReturnsTrue()
        {
            // Arrange
            _character.Clan.Returns(Substitute.For<IClan>());

            // Act
            var result = _character.HasClan();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasClan_WhenClanIsNull_ReturnsFalse()
        {
            // Arrange
            _character.Clan.Returns((IClan)null);

            // Act
            var result = _character.HasClan();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasSlayerTask_WhenTaskIdIsSet_ReturnsTrue()
        {
            // Arrange
            _slayer.CurrentTaskId.Returns(1);

            // Act
            var result = _character.HasSlayerTask();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasSlayerTask_WhenTaskIdIsNotSet_ReturnsFalse()
        {
            // Arrange
            _slayer.CurrentTaskId.Returns(-1);

            // Act
            var result = _character.HasSlayerTask();

            // Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void ResetMovementType_WhenRunIsToggled_ShouldSetMovementToRun()
        {
            // Arrange
            _profile.GetValue<bool>(ProfileConstants.RunSettingsToggled).Returns(true);

            // Act
            _character.ResetMovementType();

            // Assert
            _movement.Received().MovementType = MovementType.Run;
        }

        [TestMethod]
        public void ResetMovementType_WhenRunIsNotToggled_ShouldSetMovementToWalk()
        {
            // Arrange
            _profile.GetValue<bool>(ProfileConstants.RunSettingsToggled).Returns(false);

            // Act
            _character.ResetMovementType();

            // Assert
            _movement.Received().MovementType = MovementType.Walk;
        }

        [TestMethod]
        public void ForceRunMovementType_WhenForceRunIsTrue_ShouldSetMovementToRun()
        {
            // Arrange
            _movement.MovementType.Returns(MovementType.Walk);

            // Act
            _character.ForceRunMovementType(true);

            // Assert
            _movement.Received().MovementType = MovementType.Run;
        }

        [TestMethod]
        public void ForceRunMovementType_WhenForceRunIsFalseAndMovementIsWalk_ShouldSetMovementToWalk()
        {
            // Arrange
            _movement.MovementType.Returns(MovementType.Walk);

            // Act
            _character.ForceRunMovementType(false);

            // Assert
            _movement.Received().MovementType = MovementType.Walk;
        }

        [TestMethod]
        public void ForceRunMovementType_WhenForceRunIsFalseAndMovementIsRun_ShouldSetMovementToRun()
        {
            // Arrange
            _movement.MovementType.Returns(MovementType.Run);

            // Act
            _character.ForceRunMovementType(false);

            // Assert
            _movement.Received().MovementType = MovementType.Run;
        }

        [TestMethod]
        public void TryGetScript_WhenScriptExists_ReturnsTrueAndScript()
        {
            // Arrange
            var expectedScript = Substitute.For<IExampleScript>();
            _character.GetScript<IExampleScript>().Returns(expectedScript);

            // Act
            var result = _character.TryGetScript<IExampleScript>(out var actualScript);

            // Assert
            Assert.IsTrue(result);
            Assert.AreSame(expectedScript, actualScript);
        }

        [TestMethod]
        public void TryGetScript_WhenScriptDoesNotExist_ReturnsFalseAndNull()
        {
            // Arrange
            _character.GetScript<IExampleScript>().Returns((IExampleScript)null);

            // Act
            var result = _character.TryGetScript<IExampleScript>(out var actualScript);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(actualScript);
        }

        [TestMethod]
        public void GetOrAddScript_WhenScriptExists_ReturnsExistingScript()
        {
            // Arrange
            var expectedScript = Substitute.For<IExampleScript>();
            _character.GetScript<IExampleScript>().Returns(expectedScript);

            // Act
            var result = _character.GetOrAddScript<IExampleScript>();

            // Assert
            Assert.AreSame(expectedScript, result);
            _character.DidNotReceive().AddScript<IExampleScript>();
        }

        [TestMethod]
        public void GetOrAddScript_WhenScriptDoesNotExist_AddsAndReturnsNewScript()
        {
            // Arrange
            var newScript = Substitute.For<IExampleScript>();
            _character.GetScript<IExampleScript>().Returns((IExampleScript)null);
            _character.AddScript<IExampleScript>().Returns(newScript);

            // Act
            var result = _character.GetOrAddScript<IExampleScript>();

            // Assert
            Assert.AreSame(newScript, result);
            _character.Received(1).AddScript<IExampleScript>();
        }

        public interface IExampleScript : ICharacterScript { }
    }
}