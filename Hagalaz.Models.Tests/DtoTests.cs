using Hagalaz.Game.Network.Model;
using Hagalaz.Game.Messages.Model;
using Hagalaz.Services.Authorization.Model;
using Xunit;

namespace Hagalaz.Models.Tests
{
    public class DtoTests
    {
        [Fact]
        public void FriendsChatMemberDto_ShouldBeInitializable()
        {
            var dto = new FriendsChatMemberDto { DisplayName = "Test" };
            Assert.Equal("Test", dto.DisplayName);
        }

        [Fact]
        public void NotifyClanSettingsChanged_ShouldBeInitializable()
        {
            var dto = new NotifyClanSettingsChanged { Settings = new NotifyClanSettingsChanged.ClanSettingsDto() };
            Assert.NotNull(dto.Settings);
        }

        [Fact]
        public void HCaptchaVerifyResult_ShouldBeInitializable()
        {
            var dto = new HCaptchaVerifyResult { HostName = "localhost" };
            Assert.Equal("localhost", dto.HostName);
        }

        [Fact]
        public void FriendsChatMemberDto_ShouldHaveDefaultValues()
        {
            var dto = new FriendsChatMemberDto { DisplayName = "Test" };
            Assert.Equal(0, dto.WorldId);
            Assert.False(dto.InLobby);
        }
    }
}
