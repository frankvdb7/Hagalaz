using System.Threading.Tasks;
using Hagalaz.Configuration;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ChatSettingsFilterChangedConsumerTests
{
    [TestMethod]
    public async Task Consume_FriendsFilterChange_UpdatesContacts()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => x.AddHandler<SetContactSettingsRequest>(c => c.RespondAsync(new SetContactSettingsResponse())))
            .BuildServiceProvider(true);
        var harness = provider.GetTestHarness();
        await harness.Start();

        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Character.Returns(character);
        var context = Substitute.For<ConsumeContext<ProfileValueChanged<Availability>>>();
        context.Message.Returns(new ProfileValueChanged<Availability>(ProfileConstants.ChatSettingsFriendsFilter, Availability.Nobody, Availability.Everyone));

        await new ChatSettingsFilterChangedConsumer(accessor, provider.GetRequiredService<IBus>()).Consume(context);

        Assert.IsTrue(await harness.Consumed.Any<SetContactSettingsRequest>(x => x.Context.Message.MasterId == 42u));
    }
}
