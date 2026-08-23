using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Orbs;

[TestClass]
public sealed class RunEnergyOrbTests
{
    [TestMethod]
    public void StartResting_AddsConcreteRestingStateWithCleanup()
    {
        var character = Substitute.For<ICharacter>();
        var movement = Substitute.For<IMovement>();
        var appearance = Substitute.For<ICharacterAppearance>();
        var configurations = Substitute.For<IConfigurations>();
        var profile = Substitute.For<IProfile>();
        var context = Substitute.For<ICharacterContext>();
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        var mediator = Substitute.For<IScopedGameMediator>();
        RestingState? addedState = null;

        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);
        character.Movement.Returns(movement);
        character.Appearance.Returns(appearance);
        character.Configurations.Returns(configurations);
        character.Profile.Returns(profile);
        character.HasState<ListeningToMusicianState>().Returns(false);
        character.HasState<RestingState>().Returns(_ => addedState is not null);
        character.When(c => c.AddState(Arg.Any<IState>()))
            .Do(callInfo => addedState = callInfo.Arg<IState>() as RestingState);

        var orb = new RunEnergyOrb(contextAccessor, mediator);

        orb.StartResting();

        Assert.IsTrue(character.HasState<RestingState>());
        Assert.IsNotNull(addedState);
        Assert.AreEqual(typeof(RestingState), addedState.GetType());
        Assert.IsNotNull(addedState.OnRemovedCallback);
        movement.Received(1).ClearQueue();

        addedState.OnRemoved(character);

        character.Received(2).QueueAnimation(Arg.Any<IAnimation>());
        appearance.Received(1).ResetRenderID();
    }
}
