using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Azzanadra;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.ChiefThiefRobin;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.CombatInstructor;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Mandrith;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Oziach;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Sage;
using Hagalaz.Game.Scripts.Dialogues;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Npcs;

/// <summary>
///     Pins the NPCs whose Option 1 click simply opens one dialogue: which dialogue, whether it interrupts the
///     character, and that other clicks do not open it.
/// </summary>
[TestClass]
public sealed class DialogueNpcTests
{
    [TestMethod]
    [DataRow("azzanadra", typeof(AzzanadraDialogue), true)]
    [DataRow("robin", typeof(SellStolenGoodsDialogue), true)]
    [DataRow("instructor", typeof(CombatInstructorDialogue), false)]
    [DataRow("mandrith", typeof(MandrithDialogue), true)]
    [DataRow("oziach", typeof(OziachDialogue), false)]
    [DataRow("sage", typeof(SageDialogue), true)]
    public void Option1Click_OpensItsDialogueWithItsInterruptSetting(string npc, System.Type dialogueType, bool interrupt)
    {
        var world = new World(dialogueType);

        world.Create(npc).OnCharacterClickPerform(world.Clicker, NpcClickType.Option1Click);

        world.Clicker.Widgets.Received(1).OpenDialogue(world.Dialogue, interrupt, world.Owner);
    }

    [TestMethod]
    [DataRow("azzanadra", typeof(AzzanadraDialogue))]
    [DataRow("robin", typeof(SellStolenGoodsDialogue))]
    [DataRow("instructor", typeof(CombatInstructorDialogue))]
    [DataRow("mandrith", typeof(MandrithDialogue))]
    [DataRow("oziach", typeof(OziachDialogue))]
    [DataRow("sage", typeof(SageDialogue))]
    public void OtherClicks_DoNotOpenTheDialogue(string npc, System.Type dialogueType)
    {
        var world = new World(dialogueType);

        world.Create(npc).OnCharacterClickPerform(world.Clicker, NpcClickType.Option2Click);

        world.Clicker.Widgets.DidNotReceiveWithAnyArgs().OpenDialogue(default!, default, default!);
    }

    [TestMethod]
    public void NpcIds_AreRegisteredPerScript()
    {
        CollectionAssert.AreEqual(new[] { 9047 }, NpcIds<Azzanadra>());
        CollectionAssert.AreEqual(new[] { 11280 }, NpcIds<ChiefThiefRobin>());
        CollectionAssert.AreEqual(new[] { 944 }, NpcIds<CombatInstructor>());
        CollectionAssert.AreEqual(new[] { 6537 }, NpcIds<Mandrith>());
        CollectionAssert.AreEqual(new[] { 747 }, NpcIds<Oziach>());
        CollectionAssert.AreEqual(new[] { 2244 }, NpcIds<Sage>());
    }

    private static int[] NpcIds<T>() => typeof(T).GetCustomAttribute<NpcScriptMetaDataAttribute>()!.NpcIds.ToArray();

    private sealed class World
    {
        public World(System.Type dialogueType)
        {
            Owner = Substitute.For<INpc>();
            Clicker = Substitute.For<ICharacter>();
            Clicker.Widgets.Returns(Substitute.For<IWidgetContainer>());

            // Dialogue instances are only compared by identity, so skip their constructors.
            Dialogue = (IDialogueScript)RuntimeHelpers.GetUninitializedObject(dialogueType);
            WidgetActivator = Substitute.For<IWidgetScriptActivator>();
            WidgetActivator.Create(Arg.Any<ICharacter>(), dialogueType).Returns(Dialogue);
        }

        public INpc Owner { get; }
        public ICharacter Clicker { get; }
        public IDialogueScript Dialogue { get; }
        private IWidgetScriptActivator WidgetActivator { get; }
        private INpcService NpcService { get; } = Substitute.For<INpcService>();
        private ISimplePathFinder PathFinder { get; } = Substitute.For<ISimplePathFinder>();

        public NpcScriptBase Create(string npc) => npc switch
        {
            "azzanadra" => new Azzanadra(Owner, NpcService, PathFinder, WidgetActivator),
            "robin" => new ChiefThiefRobin(Owner, NpcService, PathFinder, WidgetActivator),
            "instructor" => new CombatInstructor(Owner, NpcService, PathFinder, WidgetActivator),
            "mandrith" => new Mandrith(Owner, NpcService, PathFinder, WidgetActivator),
            "oziach" => new Oziach(Owner, NpcService, PathFinder, WidgetActivator),
            "sage" => new Sage(Owner, NpcService, PathFinder, WidgetActivator),
            _ => throw new System.ArgumentOutOfRangeException(nameof(npc))
        };
    }
}
