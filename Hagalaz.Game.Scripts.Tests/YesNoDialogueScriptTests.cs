using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Abstractions.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Game.Scripts.Tests
{
    [TestClass]
    public class YesNoDialogueScriptTests
    {
        [TestMethod]
        public void YesNoDialogueScript_Properties_CanBeNullInit()
        {
            var accessor = new Mock<ICharacterContextAccessor>();
            var script = new YesNoDialogueScript(accessor.Object);
            // Before fix, these trigger CS8618 in constructor.
            // We just verify they exist and don't crash on instantiation.
            Assert.IsNotNull(script);
        }
    }
}
