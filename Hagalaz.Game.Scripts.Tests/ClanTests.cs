using Hagalaz.Game.Features.Clans;
using Hagalaz.Game.Abstractions.Features.Clans;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Hagalaz.Game.Scripts.Tests
{
    [TestClass]
    public class ClanTests
    {
        [TestMethod]
        public void Clan_Constructor_InitializesSettings()
        {
            var clan = new Clan("Test Clan");
            Assert.IsNotNull(clan.Settings);
        }

        [TestMethod]
        public void Clan_AlternativeConstructor_InitializesSettings()
        {
            var clan = new Clan("Test Clan", new Dictionary<uint, IClanMember>(), new Dictionary<uint, string>());
            // Before fix, _settings (and thus Settings) is null in this constructor
            Assert.IsNotNull(clan.Settings);
        }
    }
}
