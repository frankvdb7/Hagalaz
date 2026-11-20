using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Summoning;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Summoning;

[TestClass]
public sealed class SummoningScreenTests
{
    private const int Pouch = 12000;
    private const int Scroll = 12001;

    private static readonly SummoningDto Definition = new() { PouchId = Pouch, ScrollId = Scroll, SummonLevel = 1 };

    [TestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    [DataRow(ComponentClickType.Option4Click, 28)]
    public void PouchScreen_ClickOption_CreatesExpectedAmount(ComponentClickType type, int expected)
    {
        var f = new Fixture();
        var screen = new PouchScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        Assert.IsTrue(f.Click(type, Pouch));

        f.SkillService.Received(1).TryCreatePouches(f.Character, Definition, Pouch, expected);
    }

    [TestMethod]
    public void PouchScreen_InfuseX_CreatesTypedAmountAndClearsHandler()
    {
        var f = new Fixture();
        var screen = new PouchScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        f.Click(ComponentClickType.Option5Click, Pouch);
        Assert.IsNotNull(f.InputHandler);
        f.InputHandler!(7);

        f.SkillService.Received(1).TryCreatePouches(f.Character, Definition, Pouch, 7);
        Assert.IsNull(f.InputHandler);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void PouchScreen_InfuseXWithInvalidValue_DoesNothing(int value)
    {
        var f = new Fixture();
        var screen = new PouchScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        f.Click(ComponentClickType.Option5Click, Pouch);
        f.InputHandler!(value);

        f.SkillService.DidNotReceive().TryCreatePouches(Arg.Any<ICharacter>(), Arg.Any<SummoningDto>(), Arg.Any<int>(), Arg.Any<int>());
        Assert.IsNull(f.InputHandler);
    }

    [TestMethod]
    public void PouchScreen_Close_ClearsPendingXInput()
    {
        var f = new Fixture();
        var screen = new PouchScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);
        f.Click(ComponentClickType.Option5Click, Pouch);
        Assert.IsNotNull(f.InputHandler);

        screen.OnClose();

        Assert.IsNull(f.InputHandler);
    }

    [TestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    public void ScrollScreen_ClickOption_CreatesExpectedAmount(ComponentClickType type, int expected)
    {
        var f = new Fixture();
        var screen = new ScrollScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        Assert.IsTrue(f.Click(type, Scroll));

        f.SkillService.Received(1).TryCreateScrolls(f.Character, Definition, expected);
    }

    [TestMethod]
    public void ScrollScreen_TransformAll_UsesOwnedPouchCount()
    {
        var f = new Fixture();
        for (var i = 0; i < 3; i++) f.Inventory.Items.Add(SkillTaskTestSupport.CreateItem(Pouch, 1));
        var screen = new ScrollScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        f.Click(ComponentClickType.Option4Click, Scroll);

        f.SkillService.Received(1).TryCreateScrolls(f.Character, Definition, 3);
    }

    [TestMethod]
    public void ScrollScreen_TransformAllWithoutPouches_DoesNothing()
    {
        var f = new Fixture();
        var screen = new ScrollScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        f.Click(ComponentClickType.Option4Click, Scroll);

        f.SkillService.DidNotReceive().TryCreateScrolls(Arg.Any<ICharacter>(), Arg.Any<SummoningDto>(), Arg.Any<int>());
    }

    [TestMethod]
    public void ScrollScreen_TransformX_CreatesTypedAmountAndClearsHandler()
    {
        var f = new Fixture();
        var screen = new ScrollScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        f.Click(ComponentClickType.Option5Click, Scroll);
        f.InputHandler!(4);

        f.SkillService.Received(1).TryCreateScrolls(f.Character, Definition, 4);
        Assert.IsNull(f.InputHandler);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void ScrollScreen_TransformXWithInvalidValue_DoesNothing(int value)
    {
        var f = new Fixture();
        var screen = new ScrollScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);

        f.Click(ComponentClickType.Option5Click, Scroll);
        f.InputHandler!(value);

        f.SkillService.DidNotReceive().TryCreateScrolls(Arg.Any<ICharacter>(), Arg.Any<SummoningDto>(), Arg.Any<int>());
    }

    [TestMethod]
    public void ScrollScreen_Close_ClearsPendingXInput()
    {
        var f = new Fixture();
        var screen = new ScrollScreen(f.ContextAccessor, f.Items, f.SummoningService, f.SkillService);
        f.Open(screen);
        f.Click(ComponentClickType.Option5Click, Scroll);
        Assert.IsNotNull(f.InputHandler);

        screen.OnClose();

        Assert.IsNull(f.InputHandler);
    }

    private sealed class Fixture
    {
        private OnComponentClick? _click;

        public ComposedTestContainer Inventory { get; } = new(28);
        public ICharacter Character { get; } = Substitute.For<ICharacter>();
        public ICharacterContextAccessor ContextAccessor { get; } = Substitute.For<ICharacterContextAccessor>();
        public IItemService Items { get; } = Substitute.For<IItemService>();
        public ISummoningService SummoningService { get; } = Substitute.For<ISummoningService>();
        public ISummoningSkillService SkillService { get; } = Substitute.For<ISummoningSkillService>();
        public OnIntInput? InputHandler { get; set; }

        public Fixture()
        {
            Character.Inventory.Returns(Inventory);
            Character.Configurations.Returns(Substitute.For<IConfigurations>());
            var widgets = Substitute.For<IWidgetContainer>();
            widgets.IntInputHandler.Returns(_ => InputHandler);
            widgets.When(x => x.IntInputHandler = Arg.Any<OnIntInput?>()).Do(call => InputHandler = call.ArgAt<OnIntInput?>(0));
            Character.Widgets.Returns(widgets);
            var context = Substitute.For<ICharacterContext>();
            context.Character.Returns(Character);
            ContextAccessor.Context.Returns(context);
            SummoningService.FindDefinitionByPouchId(Pouch).Returns(Definition);
            SummoningService.FindDefinitionByScrollId(Scroll).Returns(Definition);
            Items.FindItemDefinitionById(Arg.Any<int>()).Returns(Substitute.For<IItemDefinition>());
        }

        public void Open(WidgetScript screen)
        {
            var widget = Substitute.For<IWidget>();
            widget.When(x => x.AttachClickHandler(16, Arg.Any<OnComponentClick>())).Do(call => _click = call.ArgAt<OnComponentClick>(1));
            screen.Initialize(widget);
            screen.OnOpen();
        }

        public bool Click(ComponentClickType type, int itemId) => _click!(16, type, itemId, 0);
    }
}
