using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Skills.Summoning;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Summoning;

[TestClass]
public sealed class SummoningSkillServiceTests
{
    [TestMethod]
    public async Task SummonFamiliar_WhenCharacterIsCancelledDuringDefinitionLookup_DoesNotMutateCharacter()
    {
        var summoningService = Substitute.For<ISummoningService>();
        var lookupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definitionLookup = new TaskCompletionSource<SummoningDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        summoningService.FindDefinitionByPouchId(123).Returns(_ =>
        {
            lookupStarted.TrySetResult(true);
            return definitionLookup.Task;
        });

        var character = Substitute.For<ICharacter>();
        var item = Substitute.For<IItem>();
        item.Id.Returns(123);
        character.ServiceProvider.Returns(new ServiceCollection()
            .AddSingleton<ISummoningService>(summoningService)
            .BuildServiceProvider());
        character.Inventory.Returns(Substitute.For<IInventoryContainer>());
        character.Statistics.Returns(Substitute.For<ICharacterStatistics>());

        var service = new SummoningSkillService(
            Substitute.For<INpcBuilder>(),
            Substitute.For<IFamiliarScriptProvider>());

        using var cancellation = new CancellationTokenSource();
        var summonTask = service.SummonFamiliar(character, item, cancellation.Token);
        await lookupStarted.Task;

        cancellation.Cancel();
        definitionLookup.SetResult(new SummoningDto
        {
            NpcId = 1,
            SummonLevel = 1,
            SummonSpawnCost = 1,
            SummonExperience = 1
        });

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => summonTask);

        character.DidNotReceive().AttachFamiliar(Arg.Any<IFamiliarScript>());
        character.Inventory.DidNotReceive().Remove(Arg.Any<IItem>(), Arg.Any<int>(), Arg.Any<bool>());
        character.Statistics.DidNotReceive().DamageSkill(Arg.Any<int>(), Arg.Any<int>());
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }
}
