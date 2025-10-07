using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class PermissionExtensionsTests
    {
        [DataTestMethod]
        [DataRow(Permission.SystemAdministrator, Permission.GameAdministrator, true)]
        [DataRow(Permission.SystemAdministrator, Permission.GameModerator, true)]
        [DataRow(Permission.SystemAdministrator, Permission.Donator, true)]
        [DataRow(Permission.SystemAdministrator, Permission.Standard, true)]
        [DataRow(Permission.GameAdministrator, Permission.SystemAdministrator, false)]
        [DataRow(Permission.GameModerator, Permission.GameAdministrator, false)]
        [DataRow(Permission.Donator, Permission.GameModerator, false)]
        [DataRow(Permission.Standard, Permission.Donator, false)]
        public void HasAtLeastXPermission_ReturnsCorrectBoolean(Permission userPermission, Permission requiredPermission, bool expected)
        {
            var result = userPermission.HasAtLeastXPermission(requiredPermission);
            Assert.AreEqual(expected, result, $"Checking if {userPermission} has at least {requiredPermission} permission failed.");
        }

        [DataTestMethod]
        [DataRow(Permission.SystemAdministrator, "SYSTEM ADMINISTRATOR")]
        [DataRow(Permission.GameAdministrator, "GAME ADMINISTRATOR")]
        [DataRow(Permission.GameModerator, "GAME MODERATOR")]
        [DataRow(Permission.Donator, "DONATOR")]
        [DataRow(Permission.Standard, "PLAYER")]
        public void ToRightsTitle_ReturnsCorrectTitle(Permission permission, string expectedTitle)
        {
            var result = permission.ToRightsTitle();
            Assert.AreEqual(expectedTitle, result, $"The title for {permission} is incorrect.");
        }

        [DataTestMethod]
        [DataRow(Permission.SystemAdministrator, 2)]
        [DataRow(Permission.GameAdministrator, 2)]
        [DataRow(Permission.GameModerator, 1)]
        [DataRow(Permission.Donator, 8)]
        [DataRow(Permission.Standard, 0)]
        public void ToClientRights_ReturnsCorrectRightsValue(Permission permission, int expectedRights)
        {
            var result = permission.ToClientRights();
            Assert.AreEqual(expectedRights, result, $"The client rights value for {permission} is incorrect.");
        }
    }
}