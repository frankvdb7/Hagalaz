using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests;

[TestClass]
public class CharacterExtensionsTests
{
    private ICharacter _character = null!;
    private ICharacterAppearance _appearance = null!;

    [TestInitialize]
    public void Initialize()
    {
        _character = Substitute.For<ICharacter>();
        _appearance = Substitute.For<ICharacterAppearance>();
    }

    [TestMethod]
    public void HasDisplayName_WithDisplayName_ReturnsTrue()
    {
        _character.PreviousDisplayName.Returns("TestName");
        Assert.IsTrue(_character.HasDisplayName());
    }

    [TestMethod]
    public void HasDisplayName_WithoutDisplayName_ReturnsFalse()
    {
        _character.PreviousDisplayName.Returns(string.Empty);
        Assert.IsFalse(_character.HasDisplayName());
    }

    [TestMethod]
    public void IsTransformedToNpc_WhenTransformed_ReturnsTrue()
    {
        _appearance.NpcId.Returns(1);
        Assert.IsTrue(_appearance.IsTransformedToNpc());
    }

    [TestMethod]
    public void IsTransformedToNpc_WhenNotTransformed_ReturnsFalse()
    {
        _appearance.NpcId.Returns(-1);
        Assert.IsFalse(_appearance.IsTransformedToNpc());
    }

    [TestMethod]
    public void HasFamiliar_WithFamiliar_ReturnsTrue()
    {
        _character.FamiliarScript.Returns(Substitute.For<IFamiliarScript>());
        Assert.IsTrue(_character.HasFamiliar());
    }

    [TestMethod]
    public void HasFamiliar_WithoutFamiliar_ReturnsFalse()
    {
        _character.FamiliarScript.Returns((IFamiliarScript)null!);
        Assert.IsFalse(_character.HasFamiliar());
    }

    [TestMethod]
    public void HasClan_WithClan_ReturnsTrue()
    {
        _character.Clan.Returns(Substitute.For<IClan>());
        Assert.IsTrue(_character.HasClan());
    }

    [TestMethod]
    public void HasClan_WithoutClan_ReturnsFalse()
    {
        _character.Clan.Returns((object)null!);
        Assert.IsFalse(_character.HasClan());
    }

    [TestMethod]
    public void HasSlayerTask_WithTask_ReturnsTrue()
    {
        var slayer = Substitute.For<ISlayer>();
        slayer.CurrentTaskId.Returns(1);
        _character.Slayer.Returns(slayer);
        Assert.IsTrue(_character.HasSlayerTask());
    }

    [TestMethod]
    public void HasSlayerTask_WithoutTask_ReturnsFalse()
    {
        var slayer = Substitute.For<ISlayer>();
        slayer.CurrentTaskId.Returns(-1);
        _character.Slayer.Returns(slayer);
        Assert.IsFalse(_character.HasSlayerTask());
    }

    [TestMethod]
    public void ForceRunMovementType_ForceRunTrue_SetsMovementToRun()
    {
        var movement = Substitute.For<IMovement>();
        movement.MovementType = MovementType.Walk;
        _character.Movement.Returns(movement);

        _character.ForceRunMovementType(true);

        Assert.AreEqual(MovementType.Run, _character.Movement.MovementType);
    }

    [TestMethod]
    public void ForceRunMovementType_ForceRunFalseAndRunToggledOn_SetsMovementToRun()
    {
        var profile = Substitute.For<IProfile>();
        profile.GetValue<bool>(Hagalaz.Configuration.ProfileConstants.RunSettingsToggled).Returns(true);
        _character.Profile.Returns(profile);

        var movement = Substitute.For<IMovement>();
        movement.MovementType = MovementType.Walk;
        _character.Movement.Returns(movement);

        _character.ForceRunMovementType(false);

        Assert.AreEqual(MovementType.Run, _character.Movement.MovementType);
    }

    [TestMethod]
    public void ForceRunMovementType_ForceRunFalseAndRunToggledOff_SetsMovementToWalk()
    {
        var profile = Substitute.For<IProfile>();
        profile.GetValue<bool>(Hagalaz.Configuration.ProfileConstants.RunSettingsToggled).Returns(false);
        _character.Profile.Returns(profile);

        var movement = Substitute.For<IMovement>();
        movement.MovementType = MovementType.Run;
        _character.Movement.Returns(movement);

        _character.ForceRunMovementType(false);

        Assert.AreEqual(MovementType.Walk, _character.Movement.MovementType);
    }

    [TestMethod]
    public void ResetMovementType_RunToggledOn_SetsMovementToRun()
    {
        var profile = Substitute.For<IProfile>();
        profile.GetValue<bool>(Hagalaz.Configuration.ProfileConstants.RunSettingsToggled).Returns(true);
        _character.Profile.Returns(profile);
        var movement = Substitute.For<IMovement>();
        _character.Movement.Returns(movement);

        _character.ResetMovementType();

        Assert.AreEqual(MovementType.Run, _character.Movement.MovementType);
    }

    [TestMethod]
    public void ResetMovementType_RunToggledOff_SetsMovementToWalk()
    {
        var profile = Substitute.For<IProfile>();
        profile.GetValue<bool>(Hagalaz.Configuration.ProfileConstants.RunSettingsToggled).Returns(false);
        _character.Profile.Returns(profile);
        var movement = Substitute.For<IMovement>();
        _character.Movement.Returns(movement);

        _character.ResetMovementType();

        Assert.AreEqual(MovementType.Walk, _character.Movement.MovementType);
    }

    [TestMethod]
    public void TryGetScript_ScriptExists_ReturnsTrueAndScript()
    {
        var expectedScript = Substitute.For<ICharacterScript>();
        _character.GetScript<ICharacterScript>().Returns(expectedScript);

        var result = _character.TryGetScript<ICharacterScript>(out var actualScript);

        Assert.IsTrue(result);
        Assert.AreSame(expectedScript, actualScript);
    }

    [TestMethod]
    public void TryGetScript_ScriptDoesNotExist_ReturnsFalseAndNull()
    {
        _character.GetScript<ICharacterScript>().Returns((ICharacterScript)null!);

        var result = _character.TryGetScript<ICharacterScript>(out var actualScript);

        Assert.IsFalse(result);
        Assert.IsNull(actualScript);
    }

    [TestMethod]
    public void GetOrAddScript_ScriptExists_ReturnsExistingScript()
    {
        var expectedScript = Substitute.For<ICharacterScript>();
        _character.GetScript<ICharacterScript>().Returns(expectedScript);

        var actualScript = _character.GetOrAddScript<ICharacterScript>();

        Assert.AreSame(expectedScript, actualScript);
        _character.DidNotReceive().AddScript<ICharacterScript>();
    }

    [TestMethod]
    public void GetOrAddScript_ScriptDoesNotExist_AddsAndReturnsNewScript()
    {
        var expectedScript = Substitute.For<ICharacterScript>();
        _character.GetScript<ICharacterScript>().Returns((ICharacterScript)null!);
        _character.AddScript<ICharacterScript>().Returns(expectedScript);

        var actualScript = _character.GetOrAddScript<ICharacterScript>();

        Assert.AreSame(expectedScript, actualScript);
        _character.Received(1).AddScript<ICharacterScript>();
    }
}
