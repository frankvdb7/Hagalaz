using System.Collections.Concurrent;
using Hagalaz.Configuration;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Logic.Characters.Consumers;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using MassTransit;
using MassTransit.Mediator;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ChatSettingsFilterChangedConsumerTests
{
    [TestMethod]
    public async Task ProfileHub_SetChatFilter_UpdatesTheFriendsFilterKey()
    {
        var mediator = Substitute.For<IScopedGameMediator>();

        await new ProfileHub(mediator).SetChatFilter(new SetChatFilterMessage { PrivateFilter = Availability.Friends });

        mediator.Received(1).Publish(Arg.Is<ProfileSetEnumAction>(a => a.Key == ProfileConstants.ChatSettingsFriendsFilter && (Availability)a.Value == Availability.Friends));
        mediator.DidNotReceive().Publish(Arg.Is<ProfileSetEnumAction>(a => a.Key.EndsWith("private_filter")));
    }

    [TestMethod]
    [DataRow(Availability.Everyone, ContactAvailability.Everyone)]
    [DataRow(Availability.Friends, ContactAvailability.Friends)]
    [DataRow(Availability.Nobody, ContactAvailability.Off)]
    public async Task FriendsFilterAction_UpdatesProfileAndSendsMatchingAvailabilityToContacts(Availability choice, ContactAvailability expected)
    {
        var requests = new ConcurrentBag<SetContactSettingsRequest>();
        var profile = Substitute.For<IProfile>();
        // A different stored value so the action is a real change for every choice.
        profile.GetValue(ProfileConstants.ChatSettingsFriendsFilter, Arg.Any<int>()).Returns(99);
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.Profile.Returns(profile);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Character.Returns(character);

        // The game mediator is a MassTransit mediator in production (no serialization); Contacts is reached over the bus.
        await using var provider = new ServiceCollection()
            .AddSingleton(accessor)
            .AddMediator(x =>
            {
                x.AddConsumer<ProfileSetEnumActionConsumer>();
                x.AddConsumer<ChatSettingsFilterChangedConsumer>();
            })
            .AddMassTransitTestHarness(x => x.AddHandler<SetContactSettingsRequest>(c =>
            {
                requests.Add(c.Message);
                return c.RespondAsync(new SetContactSettingsResponse());
            }))
            .BuildServiceProvider(true);
        var harness = provider.GetTestHarness();
        await harness.Start();

        await provider.GetRequiredService<IMediator>().Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsFriendsFilter, choice));

        Assert.IsTrue(await harness.Consumed.Any<SetContactSettingsRequest>());
        profile.Received(1).SetValue(ProfileConstants.ChatSettingsFriendsFilter, (int)choice);
        Assert.AreEqual(1, requests.Count);
        Assert.IsTrue(requests.TryPeek(out var request));
        Assert.AreEqual(42u, request.MasterId);
        Assert.AreEqual(expected, request.Settings.Availability);
    }
}
