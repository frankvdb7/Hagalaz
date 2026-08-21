using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Model.Creatures.Npcs;

[TestClass]
public sealed class FamiliarScriptBaseTests
{
    [TestMethod]
    public void AttachToSummoner_AppliesDefinitionAndRunsFamiliarSetup()
    {
        var summoner = Substitute.For<ICharacter>();
        var script = new TestFamiliarScript(
            Substitute.For<INpc>(),
            Substitute.For<ISmartPathFinder>(),
            Substitute.For<INpcService>(),
            Substitute.For<IItemService>(),
            Substitute.For<IItemBuilder>(),
            Substitute.For<IWidgetScriptActivator>());
        var definition = new SummoningDto { NpcId = 6815 };

        script.AttachToSummoner(summoner, definition);

        Assert.AreSame(summoner, script.Summoner);
        Assert.AreSame(definition, script.Definition);
        Assert.AreEqual(60, script.SpecialMovePoints);
        Assert.IsTrue(script.AttachedSetupRan);
    }

    [TestMethod]
    public void OnDestroy_DetachesTheFamiliarFromItsSummoner()
    {
        var summoner = Substitute.For<ICharacter>();
        var script = new TestFamiliarScript(
            Substitute.For<INpc>(),
            Substitute.For<ISmartPathFinder>(),
            Substitute.For<INpcService>(),
            Substitute.For<IItemService>(),
            Substitute.For<IItemBuilder>(),
            Substitute.For<IWidgetScriptActivator>());

        script.AttachToSummoner(summoner, new SummoningDto { NpcId = 6815 });
        script.OnDestroy();

        summoner.Received(1).DetachFamiliar(script.Familiar);
    }

    private sealed class TestFamiliarScript(
        INpc owner,
        ISmartPathFinder pathFinder,
        INpcService npcService,
        IItemService itemService,
        IItemBuilder itemBuilder,
        IWidgetScriptActivator widgetScriptActivator)
        : FamiliarScriptBase(owner, pathFinder, npcService, itemService, itemBuilder, widgetScriptActivator)
    {
        public bool AttachedSetupRan { get; private set; }

        protected override void OnAttachedToSummoner() => AttachedSetupRan = true;
    }
}
