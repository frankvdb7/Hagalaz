using Hagalaz.Game.Configuration;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterStatisticsTests
{
    [TestMethod]
    public void GetRunEnergyRestoreRate_RestingStateUsesRestingRecoveryRate()
    {
        var character = Substitute.For<ICharacter>();
        var movement = Substitute.For<IMovement>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        character.Movement.Returns(movement);
        character.ServiceProvider.Returns(serviceProvider);
        movement.Moving.Returns(false);
        character.HasState<RestingState>().Returns(true);
        serviceProvider.GetService(typeof(IOptions<CombatOptions>))
            .Returns(Options.Create(new CombatOptions()));
        serviceProvider.GetService(typeof(IOptions<SkillOptions>))
            .Returns(Options.Create(new SkillOptions()));

        var statistics = new CharacterStatistics(character);

        Assert.AreEqual(350, statistics.GetRunEnergyRestoreRate());
    }
}
