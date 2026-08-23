using Hagalaz.Game.Configuration;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
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
        character.Movement.Returns(movement);
        movement.Moving.Returns(false);
        character.HasState<RestingState>().Returns(true);

        var statistics = new CharacterStatistics(
            character,
            Options.Create(new CombatOptions()),
            Options.Create(new SkillOptions()),
            Substitute.For<IHitSplatBuilder>());

        Assert.AreEqual(350, statistics.GetRunEnergyRestoreRate());
    }
}
