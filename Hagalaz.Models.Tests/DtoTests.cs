using System.Linq;
using Hagalaz.Game.Network.Model;
using Hagalaz.Game.Messages.Model;
using Hagalaz.Services.Authorization.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Models.Tests
{
[TestClass]
    public class DtoTests
    {
        [TestMethod]
        public void FriendsChatMemberDto_ShouldBeInitializable()
        {
            var dto = new FriendsChatMemberDto { DisplayName = "Test" };
            Assert.AreEqual("Test", dto.DisplayName);
        }

        [TestMethod]
        public void NotifyClanSettingsChanged_ShouldBeInitializable()
        {
            var dto = new NotifyClanSettingsChanged { Settings = new NotifyClanSettingsChanged.ClanSettingsDto() };
            Assert.IsNotNull(dto.Settings);
        }

        [TestMethod]
        public void HCaptchaVerifyResult_ShouldBeInitializable()
        {
            var dto = new HCaptchaVerifyResult { HostName = "localhost" };
            Assert.AreEqual("localhost", dto.HostName);
        }

        [TestMethod]
        public void FriendsChatMemberDto_ShouldHaveDefaultValues()
        {
            var dto = new FriendsChatMemberDto { DisplayName = "Test" };
            Assert.AreEqual(0, dto.WorldId);
            Assert.IsFalse(dto.InLobby);
        }
    }
}
