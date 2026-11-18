using System.Reflection;
using System.Text;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Exceptions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Hubs;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Raido.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ContactHubTests
{
    private const uint MasterId = 42;
    private const string PrivatePayloadText = "secret-private-message";

    [TestMethod]
    public async Task AddMessage_WhenRequestSucceeds_EchoesMessageToSender()
    {
        var fixture = new Fixture();

        await fixture.Hub.AddMessage(fixture.CreateMessage());

        await fixture.AddContactMessageClient.Received(1).GetResponse<AddContactMessageResponse>(
            Arg.Any<AddContactMessageRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
        fixture.Session.Received(1).SendMessage(Arg.Any<AddContactSenderMessage>());
    }

    [TestMethod]
    public async Task AddMessage_WhenNotFound_ReportsMissingPlayer()
    {
        var fixture = new Fixture();
        fixture.FailAddContactMessage(new NotFoundException("missing"));

        await fixture.Hub.AddMessage(fixture.CreateMessage());

        fixture.Character.Received(1).SendChatMessage(string.Format(GameStrings.PlayerXDoesNotExist, "Friend"));
        fixture.Session.DidNotReceive().SendMessage(Arg.Any<AddContactSenderMessage>());
    }

    [TestMethod]
    public async Task AddMessage_WhenRequestTimesOut_SendsGenericErrorOnceAndLogsWithoutPayload()
    {
        var fixture = new Fixture();
        fixture.FailAddContactMessage(new RequestTimeoutException("req"));

        await fixture.Hub.AddMessage(fixture.CreateMessage());

        fixture.Character.Received(1).SendChatMessage(GameStrings.SomethingWentWrong);
        fixture.Session.DidNotReceive().SendMessage(Arg.Any<AddContactSenderMessage>());
        await fixture.AddContactMessageClient.Received(1).GetResponse<AddContactMessageResponse>(
            Arg.Any<AddContactMessageRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
        var entry = fixture.Logger.Entries.Single();
        Assert.AreEqual(LogLevel.Warning, entry.Level);
        StringAssert.Contains(entry.Message, "AddContactMessage");
        Assert.DoesNotContain(PrivatePayloadText, entry.Message);
        Assert.DoesNotContain(PrivatePayloadText, entry.Exception?.ToString() ?? string.Empty);
    }

    [TestMethod]
    public async Task AddMessage_WhenUnexpectedFailure_SendsGenericErrorAndLogsError()
    {
        var fixture = new Fixture();
        fixture.FailAddContactMessage(new InvalidOperationException("boom"));

        await fixture.Hub.AddMessage(fixture.CreateMessage());

        fixture.Character.Received(1).SendChatMessage(GameStrings.SomethingWentWrong);
        var entry = fixture.Logger.Entries.Single();
        Assert.AreEqual(LogLevel.Error, entry.Level);
        Assert.DoesNotContain(PrivatePayloadText, entry.Message);
    }

    [TestMethod]
    public async Task AddFriend_WhenRequestTimesOut_DoesNotMutateFriendsAndSendsOneRequest()
    {
        var fixture = new Fixture();
        fixture.FailAddContact(new RequestTimeoutException("req"));

        await fixture.Hub.AddFriend(new AddFriendMessage { DisplayName = "Friend" });

        Assert.IsFalse(fixture.Contacts.Friends.Any());
        await fixture.AddContactClient.Received(1).GetResponse<AddContactResponse, ContactMutationFailedResponse>(
            Arg.Any<AddContactRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
        fixture.AssertGenericError();
        fixture.AssertSingleLog("AddFriend", LogLevel.Warning);
    }

    [TestMethod]
    public async Task AddFriend_WhenNotFound_ReportsMissingPlayerWithoutLogging()
    {
        var fixture = new Fixture();
        fixture.FailAddContact(new NotFoundException("missing"));

        await fixture.Hub.AddFriend(new AddFriendMessage { DisplayName = "Friend" });

        Assert.IsFalse(fixture.Contacts.Friends.Any());
        Assert.IsEmpty(fixture.Logger.Entries);
        Assert.AreEqual(string.Format(GameStrings.PlayerXDoesNotExist, "Friend"), fixture.SentChatTexts.Single());
    }

    [TestMethod]
    public async Task AddIgnore_WhenRequestTimesOut_DoesNotMutateIgnores()
    {
        var fixture = new Fixture();
        fixture.FailAddContact(new RequestTimeoutException("req"));

        await fixture.Hub.AddIgnore(new AddIgnoreMessage { DisplayName = "Friend" });

        Assert.IsFalse(fixture.Contacts.Ignores.Any());
        await fixture.AddContactClient.Received(1).GetResponse<AddContactResponse, ContactMutationFailedResponse>(
            Arg.Any<AddContactRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
        fixture.AssertGenericError();
        fixture.AssertSingleLog("AddIgnore", LogLevel.Warning);
    }

    [TestMethod]
    public async Task RemoveFriend_WhenRequestTimesOut_KeepsFriendAndSendsOneRequest()
    {
        var fixture = new Fixture();
        fixture.Contacts.Friends.Add(new Friend { MasterId = 7, Rank = default, Availability = default, AreMutualFriends = false });
        fixture.FailRemoveContact(new RequestTimeoutException("req"));

        await fixture.Hub.RemoveFriend(new RemoveFriendMessage { DisplayName = "Friend" });

        Assert.IsTrue(fixture.Contacts.Friends.Contains(7));
        await fixture.RemoveContactClient.Received(1).GetResponse<RemoveContactResponse, ContactMutationFailedResponse>(
            Arg.Any<RemoveContactRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
        fixture.AssertGenericError();
        fixture.AssertSingleLog("RemoveFriend", LogLevel.Warning);
    }

    [TestMethod]
    public async Task RemoveIgnore_WhenUnexpectedFailure_KeepsIgnoreAndLogsError()
    {
        var fixture = new Fixture();
        fixture.Contacts.Ignores.Add(new Ignore { MasterId = 7 });
        fixture.FailRemoveContact(new InvalidOperationException("boom"));

        await fixture.Hub.RemoveIgnore(new RemoveIgnoreMessage { DisplayName = "Friend" });

        Assert.IsTrue(fixture.Contacts.Ignores.Contains(7));
        fixture.AssertGenericError();
        fixture.AssertSingleLog("RemoveIgnore", LogLevel.Error);
    }

    [TestMethod]
    public async Task RemoveFriend_WhenNotFound_ReportsMissingPlayerAndKeepsFriend()
    {
        var fixture = new Fixture();
        fixture.Contacts.Friends.Add(new Friend { MasterId = 7, Rank = default, Availability = default, AreMutualFriends = false });
        fixture.FailRemoveContact(new NotFoundException("missing"));

        await fixture.Hub.RemoveFriend(new RemoveFriendMessage { DisplayName = "Friend" });

        Assert.IsTrue(fixture.Contacts.Friends.Contains(7));
        Assert.AreEqual(string.Format(GameStrings.PlayerXDoesNotExist, "Friend"), fixture.SentChatTexts.Single());
        Assert.IsEmpty(fixture.Logger.Entries);
    }

    private sealed class Fixture
    {
        public IRequestClient<AddContactRequest> AddContactClient { get; } = Substitute.For<IRequestClient<AddContactRequest>>();
        public IRequestClient<RemoveContactRequest> RemoveContactClient { get; } = Substitute.For<IRequestClient<RemoveContactRequest>>();
        public IRequestClient<AddContactMessageRequest> AddContactMessageClient { get; } = Substitute.For<IRequestClient<AddContactMessageRequest>>();
        public ICharacter Character { get; } = Substitute.For<ICharacter>();
        public IGameSession Session { get; } = Substitute.For<IGameSession>();
        public ContactsFeature Contacts { get; } = new();
        public CapturingLogger Logger { get; } = new();
        public List<string> SentChatTexts { get; } = [];
        public ContactHub Hub { get; }

        public Fixture()
        {
            Character.MasterId.Returns(MasterId);
            Character.Session.Returns(Session);
            Contacts.CompleteInitialSnapshot();

            var proxy = Substitute.For<IRaidoClientProxy>();
            proxy.SendAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    SentChatTexts.Add(call.Arg<ChatMessage>().Text);
                    return Task.CompletedTask;
                });
            var clients = Substitute.For<IRaidoCallerClients>();
            clients.Caller.Returns(proxy);

            var features = new FeatureCollection();
            features.Set<ICharacterFeature>(new CharacterFeature { Character = Character });
            features.Set<IContactsFeature>(Contacts);
            features.Set<IAuthenticationFeature>(new AuthenticationFeature
            {
                AuthenticationProperties = new AuthenticationProperties
                {
                    Claims = new Dictionary<string, object> { [Claims.Subject] = MasterId.ToString() }
                }
            });
            var context = Substitute.For<RaidoCallerContext>();
            context.Features.Returns(features);

            Hub = new ContactHub(AddContactClient, RemoveContactClient, AddContactMessageClient, Substitute.For<IMapper>(), Logger);
            typeof(RaidoHub).GetProperty(nameof(RaidoHub.Context), BindingFlags.Instance | BindingFlags.Public)!.SetValue(Hub, context);
            typeof(RaidoHub).GetProperty(nameof(RaidoHub.Clients), BindingFlags.Instance | BindingFlags.Public)!.SetValue(Hub, clients);
        }

        public AddContactMessage CreateMessage() => new()
        {
            ContactDisplayName = "Friend",
            MessageLength = 3,
            MessagePayload = new System.Buffers.ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(PrivatePayloadText))
        };

        public void FailAddContactMessage(Exception exception) =>
            AddContactMessageClient
                .GetResponse<AddContactMessageResponse>(Arg.Any<AddContactMessageRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
                .Returns(Task.FromException<Response<AddContactMessageResponse>>(exception));

        public void FailAddContact(Exception exception) =>
            AddContactClient
                .GetResponse<AddContactResponse, ContactMutationFailedResponse>(Arg.Any<AddContactRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
                .Returns(Task.FromException<Response<AddContactResponse, ContactMutationFailedResponse>>(exception));

        public void FailRemoveContact(Exception exception) =>
            RemoveContactClient
                .GetResponse<RemoveContactResponse, ContactMutationFailedResponse>(Arg.Any<RemoveContactRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
                .Returns(Task.FromException<Response<RemoveContactResponse, ContactMutationFailedResponse>>(exception));

        public void AssertGenericError() => Assert.AreEqual(GameStrings.SomethingWentWrong, SentChatTexts.Single());

        public void AssertSingleLog(string operation, LogLevel level)
        {
            var entry = Logger.Entries.Single();
            Assert.AreEqual(level, entry.Level);
            StringAssert.Contains(entry.Message, operation);
        }
    }

    private sealed class CapturingLogger : ILogger<ContactHub>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
