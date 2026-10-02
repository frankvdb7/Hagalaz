using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Prayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Prayer;

[TestClass]
public sealed class PrayerCallerLookupFailureTests
{
    [DataTestMethod]
    [DataRow(false, 526)]
    [DataRow(true, 592)]
    public async Task ConcretePrayerCaller_WhenDefinitionIsMissingDoesNotStartAction(bool ash, int id)
    {
        var (character, inventory, item, run) = Create(ash, id);
        var service = Substitute.For<IPrayerService>();
        service.FindById(id).Returns(Task.FromResult<PrayerDto?>(null));
        Queue(ash, service, character, item);

        await run(CancellationToken.None);

        Assert.AreSame(item, inventory.Items[0]);
        character.DidNotReceive().AddState(Arg.Any<IState>());
        character.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());
    }

    [DataTestMethod]
    [DataRow(false, 526)]
    [DataRow(true, 592)]
    public async Task ConcretePrayerCaller_WhenItemDisappearsDuringLookupDoesNotStartAction(bool ash, int id)
    {
        var (character, inventory, item, run) = Create(ash, id);
        var completion = new TaskCompletionSource<PrayerDto?>();
        var service = Substitute.For<IPrayerService>();
        service.FindById(id).Returns(completion.Task);
        Queue(ash, service, character, item);

        var pending = run(CancellationToken.None);
        inventory.Items.Remove(item, 0);
        completion.SetResult(new PrayerDto { ItemId = id, Experience = 4.5, Type = ash ? PrayerDtoType.Ashes : PrayerDtoType.Bones });
        await pending;

        character.DidNotReceive().AddState(Arg.Any<IState>());
        character.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());
    }

    private static (ICharacter, ComposedTestContainer, IItem, Func<CancellationToken, Task>) Create(bool ash, int id)
    {
        var inventory = new ComposedTestContainer(2); var item = ComposedTestContainer.CreateTestItem(id); inventory.SetItem(0, item);
        var character = Substitute.For<ICharacter>(); character.Inventory.Returns(inventory);
        Func<CancellationToken, Task>? queued = null;
        character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(task => queued = task)).Returns(Substitute.For<IRsTaskHandle>());
        Func<CancellationToken, Task> run = token => queued!(token);
        return (character, inventory, item, run);
    }

    private static void Queue(bool ash, IPrayerService service, ICharacter character, IItem item)
    {
        if (ash) new StandardAsh(service).ItemClickedInInventory(ComponentClickType.LeftClick, item, character);
        else new StandardBones(service).ItemClickedInInventory(ComponentClickType.LeftClick, item, character);
    }
}
