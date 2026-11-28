using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Scripts.Areas.Wilderness;
using Hagalaz.Game.Scripts.Areas.Wilderness.Npcs;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Areas;

[TestClass]
public sealed class WildernessAreaScriptTests
{
    [TestMethod]
    public void OnCreatureExitArea_WhenDestinationUsesWildernessScript_KeepsWildernessCharacterScript()
    {
        var character = Substitute.For<ICharacter>();
        var destination = Substitute.For<IArea>();
        destination.Script.Returns(new Wilderness(Options.Create(new WorldOptions())));
        character.Area.Returns(destination);
        var wilderness = new Wilderness(Options.Create(new WorldOptions()));

        wilderness.OnCreatureExitArea(character);

        character.DidNotReceive().TryRemoveScript<WildernessScript>();
    }

    [TestMethod]
    public void RenderExitArea_WhenDestinationUsesWildernessScript_LeavesOverlayOpen()
    {
        var character = Substitute.For<ICharacter>();
        var widgets = Substitute.For<IWidgetContainer>();
        var overlay = Substitute.For<IWidget>();
        widgets.GetOpenWidget(381).Returns(overlay);
        character.Widgets.Returns(widgets);
        var destination = Substitute.For<IArea>();
        destination.Script.Returns(new Wilderness(Options.Create(new WorldOptions())));
        character.Area.Returns(destination);
        var wilderness = new Wilderness(Options.Create(new WorldOptions()));

        wilderness.RenderExitArea(character);

        widgets.DidNotReceive().CloseWidget(overlay);
        character.Appearance.DidNotReceive().Refresh();
    }
}
