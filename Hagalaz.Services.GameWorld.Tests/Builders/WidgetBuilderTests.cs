using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Builders
{
    [TestClass]
    public class WidgetBuilderTests
    {
        [TestMethod]
        public void Build_WhenCurrentFrameIsNull_SetsParentToZero()
        {
            var scriptProvider = Substitute.For<IWidgetScriptProvider>();
            var character = Substitute.For<ICharacter>();
            character.Widgets.CurrentFrame.Returns((IWidget?)null);
            var builder = new WidgetBuilder(scriptProvider);
            builder.ForCharacter(character);
            builder.WithId(1);

            var widget = builder.Build();

            Assert.AreEqual(0, widget.ParentId);
        }
    }
}
