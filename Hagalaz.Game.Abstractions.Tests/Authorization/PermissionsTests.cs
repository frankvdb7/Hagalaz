using Hagalaz.Game.Abstractions.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests.Authorization;

[TestClass]
public class PermissionHelpersTests
{
    [TestMethod]
    public void ParseRole_WithValidSystemAdministratorRole_ReturnsSystemAdministratorPermission()
    {
        // Arrange
        var role = "SystemAdministrator";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.SystemAdministrator, result);
    }

    [TestMethod]
    public void ParseRole_WithValidGameAdministratorRole_ReturnsGameAdministratorPermission()
    {
        // Arrange
        var role = "GameAdministrator";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.GameAdministrator, result);
    }

    [TestMethod]
    public void ParseRole_WithValidGameModeratorRole_ReturnsGameModeratorPermission()
    {
        // Arrange
        var role = "GameModerator";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.GameModerator, result);
    }

    [TestMethod]
    public void ParseRole_WithValidDonatorRole_ReturnsDonatorPermission()
    {
        // Arrange
        var role = "Donator";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.Donator, result);
    }

    [TestMethod]
    public void ParseRole_WithValidStandardRole_ReturnsStandardPermission()
    {
        // Arrange
        var role = "Standard";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.Standard, result);
    }

    [TestMethod]
    public void ParseRole_WithInvalidRole_ReturnsStandardPermission()
    {
        // Arrange
        var role = "InvalidRole";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.Standard, result);
    }

    [TestMethod]
    public void ParseRole_WithEmptyRole_ReturnsStandardPermission()
    {
        // Arrange
        var role = "";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.Standard, result);
    }

    [TestMethod]
    public void ParseRole_WithNullRole_ReturnsStandardPermission()
    {
        // Arrange
        string? role = null;

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.Standard, result);
    }

    [TestMethod]
    public void ParseRole_WithCaseInsensitiveRole_ReturnsGameAdministratorPermission()
    {
        // Arrange
        var role = "gameadministrator";

        // Act
        var result = PermissionHelpers.ParseRole(role);

        // Assert
        Assert.AreEqual(Permission.GameAdministrator, result);
    }
}
