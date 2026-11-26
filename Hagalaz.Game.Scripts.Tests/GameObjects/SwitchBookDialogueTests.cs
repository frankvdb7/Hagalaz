using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.GameObjects.Altars;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.Widgets;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.GameObjects;

/// <summary>
///     Pins the altar dialogues that switch spellbook or prayer book: which book each altar selects, that a second use
///     switches back to the standard book, and that cancelling changes nothing.
/// </summary>
[TestClass]
public sealed class SwitchBookDialogueTests
{
    private const string SpellOption = "Yes, replace my spellbook.";
    private const string PrayerOption = "Yes, replace my prayer book.";

    [TestMethod]
    [DataRow(MagicBook.StandardBook, MagicBook.AncientBook)]
    [DataRow(MagicBook.LunarBook, MagicBook.AncientBook)]
    [DataRow(MagicBook.AncientBook, MagicBook.StandardBook)]
    public void AncientAltar_SwitchesToAncientOrBackToStandard(MagicBook current, MagicBook expected)
    {
        var world = new World();
        world.Character.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook).Returns(current);
        var dialogue = world.Open((context, mediator) => new SwitchAncientBookDialogue(context, mediator));

        dialogue.OnDialogueOptionClick(SpellOption, -1, -1);

        world.AssertPublished(ProfileConstants.MagicSettingsBook, expected);
    }

    [TestMethod]
    [DataRow(MagicBook.StandardBook, MagicBook.LunarBook)]
    [DataRow(MagicBook.AncientBook, MagicBook.LunarBook)]
    [DataRow(MagicBook.LunarBook, MagicBook.StandardBook)]
    public void LunarAltar_SwitchesToLunarOrBackToStandard(MagicBook current, MagicBook expected)
    {
        var world = new World();
        world.Character.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook).Returns(current);
        var dialogue = world.Open((context, mediator) => new SwitchLunarBookDialogue(context, mediator));

        dialogue.OnDialogueOptionClick(SpellOption, -1, -1);

        world.AssertPublished(ProfileConstants.MagicSettingsBook, expected);
    }

    [TestMethod]
    [DataRow(PrayerBook.StandardBook, PrayerBook.CursesBook)]
    [DataRow(PrayerBook.CursesBook, PrayerBook.StandardBook)]
    public void AltarOfZaros_SwitchesBetweenStandardPrayersAndCurses(PrayerBook current, PrayerBook expected)
    {
        var world = new World();
        world.Character.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook).Returns(current);
        var dialogue = world.Open((context, mediator) => new SwitchPrayerBookDialogue(context, mediator));

        dialogue.OnDialogueOptionClick(PrayerOption, -1, -1);

        world.AssertPublished(ProfileConstants.PrayerSettingsBook, expected);
    }

    [TestMethod]
    [DataRow("ancient")]
    [DataRow("lunar")]
    [DataRow("prayer")]
    public void Nevermind_ClosesTheChatboxAndChangesNothing(string altar)
    {
        var world = new World();
        DialogueScript dialogue = altar switch
        {
            "ancient" => world.Open((context, mediator) => new SwitchAncientBookDialogue(context, mediator)),
            "lunar" => world.Open((context, mediator) => new SwitchLunarBookDialogue(context, mediator)),
            _ => world.Open((context, mediator) => new SwitchPrayerBookDialogue(context, mediator))
        };

        dialogue.OnDialogueOptionClick("Nevermind.", -1, -1);

        world.Character.Widgets.Received(1).CloseChatboxOverlay();
        world.Mediator.DidNotReceiveWithAnyArgs().Publish(Arg.Any<ProfileSetEnumAction>());
    }

    [TestMethod]
    public void SetSource_AcceptsGameObjectsOnly()
    {
        var world = new World();
        var dialogue = world.Open((context, mediator) => new SwitchAncientBookDialogue(context, mediator));

        dialogue.SetSource(Substitute.For<Hagalaz.Game.Abstractions.Model.GameObjects.IGameObject>());

        Assert.ThrowsExactly<System.Exception>(() => dialogue.SetSource(Substitute.For<Hagalaz.Game.Abstractions.Model.IRuneObject>()));
    }


    // ---------- Altar objects that open the dialogues ----------

    [TestMethod]
    [DataRow("ancient", GameObjectClickType.Option1Click, true)]
    [DataRow("ancient", GameObjectClickType.Option2Click, false)]
    [DataRow("lunar", GameObjectClickType.Option2Click, true)]
    [DataRow("lunar", GameObjectClickType.Option1Click, false)]
    [DataRow("zaros", GameObjectClickType.Option1Click, true)]
    [DataRow("zaros", GameObjectClickType.Option2Click, false)]
    public void Altar_OpensItsDialogueOnlyOnItsSwitchClick(string altar, GameObjectClickType click, bool opens)
    {
        var world = new World();
        var provider = Substitute.For<IServiceProvider>();
        var ancient = new SwitchAncientBookDialogue(world.ContextAccessor, world.Mediator);
        var lunar = new SwitchLunarBookDialogue(world.ContextAccessor, world.Mediator);
        var prayer = new SwitchPrayerBookDialogue(world.ContextAccessor, world.Mediator);
        provider.GetService(typeof(SwitchAncientBookDialogue)).Returns(ancient);
        provider.GetService(typeof(SwitchLunarBookDialogue)).Returns(lunar);
        provider.GetService(typeof(SwitchPrayerBookDialogue)).Returns(prayer);
        world.Character.ServiceProvider.Returns(provider);
        var owner = Substitute.For<IGameObject>();
        GameObjectScript script = altar switch
        {
            "ancient" => new AncientAltar(),
            "lunar" => new LunarAltar(),
            _ => new AltarOfZaros()
        };
        script.Initialize(owner);
        DialogueScript expected = altar switch { "ancient" => ancient, "lunar" => lunar, _ => prayer };

        script.OnCharacterClickPerform(world.Character, click);

        if (opens)
        {
            world.Character.Widgets.Received(1).OpenChatboxOverlay((short)DialogueInterfaces.Send2Options, 0, expected, true, owner);
        }
        else
        {
            world.Character.Widgets.DidNotReceiveWithAnyArgs().OpenChatboxOverlay(default, default, default!, default, default);
        }
    }

    private sealed class World
    {
        public World()
        {
            Character = Substitute.For<ICharacter>();
            Character.Widgets.Returns(Substitute.For<IWidgetContainer>());
            Character.Configurations.Returns(Substitute.For<IConfigurations>());
            Context = Substitute.For<ICharacterContext>();
            Context.Character.Returns(Character);
            ContextAccessor = Substitute.For<ICharacterContextAccessor>();
            ContextAccessor.Context.Returns(Context);
        }

        public ICharacter Character { get; }
        public ICharacterContext Context { get; }
        public ICharacterContextAccessor ContextAccessor { get; }
        public IScopedGameMediator Mediator { get; } = Substitute.For<IScopedGameMediator>();

        public T Open<T>(System.Func<ICharacterContextAccessor, IScopedGameMediator, T> create) where T : DialogueScript
        {
            var dialogue = create(ContextAccessor, Mediator);
            dialogue.Initialize(Substitute.For<IWidget>());
            dialogue.OnOpen();
            return dialogue;
        }

        public void AssertPublished(string key, System.Enum value) =>
            Mediator.Received(1).Publish(Arg.Is<ProfileSetEnumAction>(a => a.Key == key && a.Value.Equals(value)));
    }
}
