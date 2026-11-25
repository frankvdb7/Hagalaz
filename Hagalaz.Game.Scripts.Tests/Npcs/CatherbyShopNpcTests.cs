using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Catherby.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Npcs;

[TestClass]
public sealed class CatherbyShopNpcTests
{
    [TestMethod]
    [DataRow(typeof(Arhein), 22)]
    [DataRow(typeof(CandleMaker), 23)]
    [DataRow(typeof(Harry), 21)]
    [DataRow(typeof(Hickton), 20)]
    [DataRow(typeof(Vanessa), 24)]
    public void TradeOption_PublishesTheNpcShop(Type scriptType, int shopId)
    {
        var clicker = Substitute.For<ICharacter>();
        var events = Substitute.For<IEventManager>();
        clicker.EventManager.Returns(events);
        var script = Activator.CreateInstance(scriptType, Substitute.For<INpc>(), Substitute.For<INpcService>(),
            Substitute.For<ISimplePathFinder>(), Substitute.For<IWidgetScriptActivator>()) as NpcScriptBase;
        Assert.IsNotNull(script);

        script.OnCharacterClickPerform(clicker, NpcClickType.Option3Click);

        events.Received(1).SendEvent(Arg.Is<OpenShopEvent>(e => e != null && e.ShopId == shopId));
    }
}
