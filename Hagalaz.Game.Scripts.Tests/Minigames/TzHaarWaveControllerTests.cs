using System.Runtime.CompilerServices;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Minigames.TzHaar.Cave;
using Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.Dialogues;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames;

[TestClass]
public sealed class TzHaarWaveControllerTests
{
    [TestMethod]
    public void StopWaves_BeforeFinalWave_AddsExpectedTokkulReward()
    {
        var character = Substitute.For<ICharacter>();
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.AddItem(Arg.Any<IItem>()).Returns(true);
        character.Inventory.Returns(inventory);
        character.Movement.Returns(Substitute.For<IMovement>());
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());

        var itemOptional = Substitute.For<IItemOptional>();
        itemOptional.WithCount(Arg.Any<int>()).Returns(itemOptional);
        var itemId = Substitute.For<IItemId>();
        itemId.WithId(Arg.Any<int>()).Returns(itemOptional);
        var itemBuilder = Substitute.For<IItemBuilder>();
        itemBuilder.Create().Returns(itemId);
        itemOptional.Build().Returns(Substitute.For<IItem>());

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IItemBuilder)).Returns(itemBuilder);
        services.GetService(typeof(IGroundItemBuilder)).Returns(Substitute.For<IGroundItemBuilder>());
        services.GetService(typeof(LoseDialogue)).Returns(RuntimeHelpers.GetUninitializedObject(typeof(LoseDialogue)));
        character.ServiceProvider.Returns(services);

        var controller = new TzHaarWaveController(
            character,
            Substitute.For<IGameMessageService>(),
            Substitute.For<INpcBuilder>(),
            [new WaveDto { WaveId = 63, Npcs = [] }],
            []);
        controller.CurrentWaveId = 62;

        controller.StopWaves();

        itemId.Received(1).WithId(6529);
        itemOptional.Received(1).WithCount(7841);
    }
}
