using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Dwarf;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Ignatius;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Jered;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.MasterCrafter;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.MasterFisher;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Wilfred;
using Hagalaz.Game.Scripts.Areas.Guilds.Runecrafting.Npcs;
using Hagalaz.Game.Scripts.Areas.Guilds.Warrior.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;
using SkillCapeDialogue = Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue.SkillCapeDialogue;

namespace Hagalaz.Game.Scripts.Tests.Npcs;

/// <summary>
///     Pins the NPCs that sell a skill cape: clicking Option 1 opens the skill cape dialogue for the NPC's skill.
///     Wilfred is registered with an item (not NPC) script attribute in the original code; that is kept as is.
/// </summary>
[TestClass]
public sealed class SkillCapeMasterTests
{
    [TestMethod]
    [DataRow("dwarf", StatisticsConstants.Mining)]
    [DataRow("ignatius", StatisticsConstants.Firemaking)]
    [DataRow("jered", StatisticsConstants.Prayer)]
    [DataRow("crafter", StatisticsConstants.Crafting)]
    [DataRow("fisher", StatisticsConstants.Fishing)]
    [DataRow("wilfred", StatisticsConstants.Woodcutting)]
    [DataRow("larriar", StatisticsConstants.Runecrafting)]
    [DataRow("ajjat", StatisticsConstants.Attack)]
    [DataRow("sloane", StatisticsConstants.Strength)]
    public void Option1Click_OpensSkillCapeDialogueForTheNpcsSkill(string npc, int skill)
    {
        var world = new World();

        world.Create(npc).OnCharacterClickPerform(world.Clicker, NpcClickType.Option1Click);

        Assert.AreEqual(skill, world.Dialogue.SkillID);
        world.Clicker.Widgets.Received(1).OpenDialogue(world.Dialogue, true, world.Owner);
    }

    [TestMethod]
    [DataRow("dwarf")]
    [DataRow("ignatius")]
    [DataRow("jered")]
    [DataRow("crafter")]
    [DataRow("fisher")]
    [DataRow("wilfred")]
    [DataRow("larriar")]
    [DataRow("ajjat")]
    [DataRow("sloane")]
    public void OtherClicks_DoNotOpenTheDialogue(string npc)
    {
        var world = new World();

        world.Create(npc).OnCharacterClickPerform(world.Clicker, NpcClickType.Option2Click);

        world.Clicker.Widgets.DidNotReceiveWithAnyArgs().OpenDialogue(default!, default, default!);
        world.WidgetActivator.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [TestMethod]
    public void NpcIds_AreRegisteredPerScript()
    {
        CollectionAssert.AreEqual(new[] { 3295 }, NpcIds<Dwarf>());
        CollectionAssert.AreEqual(new[] { 4946 }, NpcIds<IgnatiusVulcan>());
        CollectionAssert.AreEqual(new[] { 802 }, NpcIds<BrotherJered>());
        CollectionAssert.AreEqual(new[] { 805 }, NpcIds<MasterCrafter>());
        CollectionAssert.AreEqual(new[] { 308 }, NpcIds<MasterFisher>());
        CollectionAssert.AreEqual(new[] { 13632 }, NpcIds<Larriar>());
        CollectionAssert.AreEqual(new[] { 4288 }, NpcIds<Ajjat>());
        CollectionAssert.AreEqual(new[] { 4297 }, NpcIds<Sloane>());
    }

    [TestMethod]
    public void Wilfred_IsRegisteredAsAnNpcScript()
    {
        CollectionAssert.AreEqual(new[] { 4906 }, NpcIds<Wilfred>());
        Assert.IsFalse(typeof(Wilfred).GetCustomAttributes().Any(a => a.GetType().Name == "ItemScriptMetaDataAttribute"));
    }

    private static int[] NpcIds<T>() => typeof(T).GetCustomAttribute<NpcScriptMetaDataAttribute>()!.NpcIds.ToArray();

    private sealed class World
    {
        public World()
        {
            Owner = Substitute.For<INpc>();
            Clicker = Substitute.For<ICharacter>();
            Clicker.Widgets.Returns(Substitute.For<IWidgetContainer>());

            var context = Substitute.For<ICharacterContext>();
            context.Character.Returns(Clicker);
            var contextAccessor = Substitute.For<ICharacterContextAccessor>();
            contextAccessor.Context.Returns(context);
            Dialogue = new SkillCapeDialogue(contextAccessor, Substitute.For<IItemBuilder>());

            WidgetActivator = Substitute.For<IWidgetScriptActivator>();
            WidgetActivator.Create(Arg.Any<ICharacter>(), typeof(SkillCapeDialogue)).Returns(Dialogue);
        }

        public INpc Owner { get; }
        public ICharacter Clicker { get; }
        public SkillCapeDialogue Dialogue { get; }
        public IWidgetScriptActivator WidgetActivator { get; }
        private INpcService NpcService { get; } = Substitute.For<INpcService>();
        private ISimplePathFinder PathFinder { get; } = Substitute.For<ISimplePathFinder>();

        public NpcScriptBase Create(string npc) => npc switch
        {
            "dwarf" => new Dwarf(Owner, NpcService, PathFinder, WidgetActivator),
            "ignatius" => new IgnatiusVulcan(Owner, NpcService, PathFinder, WidgetActivator),
            "jered" => new BrotherJered(Owner, NpcService, PathFinder, WidgetActivator),
            "crafter" => new MasterCrafter(Owner, NpcService, PathFinder, WidgetActivator),
            "fisher" => new MasterFisher(Owner, NpcService, PathFinder, WidgetActivator),
            "wilfred" => new Wilfred(Owner, NpcService, PathFinder, WidgetActivator),
            "larriar" => new Larriar(Owner, NpcService, PathFinder, WidgetActivator),
            "ajjat" => new Ajjat(Owner, NpcService, PathFinder, WidgetActivator),
            "sloane" => new Sloane(Owner, NpcService, PathFinder, WidgetActivator),
            _ => throw new System.ArgumentOutOfRangeException(nameof(npc))
        };
    }
}
