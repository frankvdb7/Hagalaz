using Hagalaz.Game.Features.Clans;

namespace Hagalaz.Game.Scripts.Tests
{
    [TestClass]
    public class ClanMemberTests
    {
        [TestMethod]
        public void ClanMember_DefaultDisplayName_IsNotEmpty()
        {
            var member = new ClanMember();
            Assert.IsNotNull(member.DisplayName);
        }
    }
}
