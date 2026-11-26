using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects;
using Hagalaz.Game.Scripts.Areas.SlayerTower.GameObjects;
using Hagalaz.Game.Scripts.GameObjects.Ladders;
using Hagalaz.Game.Scripts.Model.GameObjects;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.GameObjects;

/// <summary>
///     Pins the stair, step and ladder objects that climb one plane up or down, optionally shifting the character
///     horizontally. Expectations are the literals of the original per-object implementations.
/// </summary>
[TestClass]
public sealed class ClimbObjectTests
{
    private const int StartX = 100;
    private const int StartY = 200;
    private const int StartPlane = 1;
    private const int Dimension = 3;

    // name, climb-down X/Y shift (climbing up applies the opposite shift)
    [TestMethod]
    [DataRow("ladder", 0, 0)]
    [DataRow("staircase", 0, 3)]
    [DataRow("steps", 4, 0)]
    [DataRow("stepsTwo", 0, 4)]
    [DataRow("slayerFirst", 5, 0)]
    [DataRow("slayerSecond", -5, 0)]
    public void ClimbDown_AnimatesThenTeleportsDownWithShiftAfterTwoTicks(string script, int shiftX, int shiftY)
    {
        var world = new World(climbDown: true);

        world.Click(script);

        Assert.AreEqual(827, world.Animations.Single());
        world.AssertTeleportAfterTwoTicks(StartX + shiftX, StartY + shiftY, StartPlane - 1);
    }

    [TestMethod]
    [DataRow("ladder", 0, 0)]
    [DataRow("staircase", 0, 3)]
    [DataRow("steps", 4, 0)]
    [DataRow("stepsTwo", 0, 4)]
    [DataRow("slayerFirst", 5, 0)]
    [DataRow("slayerSecond", -5, 0)]
    public void ClimbUp_AnimatesThenTeleportsUpWithOppositeShiftAfterTwoTicks(string script, int downShiftX, int downShiftY)
    {
        var world = new World(climbDown: false);

        world.Click(script);

        Assert.AreEqual(828, world.Animations.Single());
        world.AssertTeleportAfterTwoTicks(StartX - downShiftX, StartY - downShiftY, StartPlane + 1);
    }

    [TestMethod]
    [DataRow("ladder")]
    [DataRow("staircase")]
    [DataRow("steps")]
    [DataRow("stepsTwo")]
    [DataRow("slayerFirst")]
    [DataRow("slayerSecond")]
    public void Click_InterruptsAndLocksMovementWithoutTeleportingBeforeTheTask(string script)
    {
        var world = new World(climbDown: true);

        world.Click(script);

        world.Clicker.Received(1).Interrupt(Arg.Any<object>());
        world.Clicker.Movement.Received(1).Lock(true);
        world.Clicker.Movement.DidNotReceive().Teleport(Arg.Any<ITeleport>());
        world.Clicker.Movement.DidNotReceive().Unlock(Arg.Any<bool>());
        Assert.AreEqual(1, world.Tasks.Count);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, true)]
    public void Task_TeleportsOnlyOnTheSecondTick(int ticks, bool teleported)
    {
        var world = new World(climbDown: true);
        world.Click("staircase");

        for (var i = 0; i < ticks; i++)
        {
            world.Tasks.Single().Tick();
        }

        Assert.AreEqual(teleported ? 1 : 0, world.Teleports.Count);
    }

    [TestMethod]
    public void IdsAreRegisteredPerObject()
    {
        CollectionAssert.AreEquivalent(new[] { 26982, 26983, 69499, 69502, 69504, 69505 }, Ids<LadderObjectScript>());
        CollectionAssert.AreEquivalent(new[] { 25339, 25340 }, Ids<StairCase>());
        CollectionAssert.AreEquivalent(new[] { 25336, 25338 }, Ids<Steps>());
        CollectionAssert.AreEquivalent(new[] { 25337, 39468 }, Ids<StepsTwo>());
        CollectionAssert.AreEquivalent(new[] { 4493, 4494 }, Ids<StaircaseFirstFloor>());
        CollectionAssert.AreEquivalent(new[] { 4495, 4496 }, Ids<StaircaseSecondFloor>());
    }

    private static int[] Ids<T>() => typeof(T).GetCustomAttributes(typeof(GameObjectScriptMetaDataAttribute), false)
        .Cast<GameObjectScriptMetaDataAttribute>().SelectMany(a => a.ObjectIds).ToArray();

    private sealed class World
    {
        public World(bool climbDown)
        {
            Owner = Substitute.For<IGameObject>();
            Owner.Definition.Actions.Returns(climbDown ? ["Climb-down", "Examine"] : ["Climb-up", "Examine"]);

            Clicker = Substitute.For<ICharacter>();
            Clicker.Location.Returns(Location.Create(StartX, StartY, StartPlane, Dimension));
            Clicker.When(c => c.QueueAnimation(Arg.Any<IAnimation>())).Do(c => Animations.Add(c.Arg<IAnimation>().Id));
            Clicker.When(c => c.QueueTask(Arg.Any<ITaskItem>())).Do(c => Tasks.Add((RsTask)c.Arg<ITaskItem>()));
            Clicker.Movement.When(m => m.Teleport(Arg.Any<ITeleport>())).Do(c => Teleports.Add(c.Arg<ITeleport>()));
        }

        public IGameObject Owner { get; }
        public ICharacter Clicker { get; }
        public List<int> Animations { get; } = [];
        public List<RsTask> Tasks { get; } = [];
        public List<ITeleport> Teleports { get; } = [];

        public void Click(string script)
        {
            GameObjectScript instance = script switch
            {
                "ladder" => new LadderObjectScript(),
                "staircase" => new StairCase(),
                "steps" => new Steps(),
                "stepsTwo" => new StepsTwo(),
                "slayerFirst" => new StaircaseFirstFloor(),
                "slayerSecond" => new StaircaseSecondFloor(),
                _ => throw new System.ArgumentOutOfRangeException(nameof(script))
            };
            instance.Initialize(Owner);
            instance.OnCharacterClickPerform(Clicker, GameObjectClickType.Option1Click);
        }

        public void AssertTeleportAfterTwoTicks(int x, int y, int plane)
        {
            var task = Tasks.Single();
            task.Tick();
            Assert.AreEqual(0, Teleports.Count);
            task.Tick();

            var location = Teleports.Single().Location;
            Assert.AreEqual(x, location.X);
            Assert.AreEqual(y, location.Y);
            Assert.AreEqual(plane, location.Z);
            Assert.AreEqual(Dimension, location.Dimension);
            Clicker.Movement.Received(1).Unlock(false);
        }
    }
}
