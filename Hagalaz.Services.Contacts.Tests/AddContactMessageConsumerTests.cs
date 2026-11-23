using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Exceptions;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Services.Model;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;
using Moq;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public sealed class AddContactMessageConsumerTests
{
    private const uint SenderId = 1;
    private const uint ReceiverId = 2;

    private Mock<IContactService> _contactService = null!;
    private Mock<ICharacterService> _characterService = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ConsumeContext<AddContactMessageRequest>> _context = null!;
    private AddContactMessageConsumer _consumer = null!;

    [TestInitialize]
    public void Initialize()
    {
        _contactService = new Mock<IContactService>();
        _characterService = new Mock<ICharacterService>();
        _mapper = new Mock<IMapper>();

        var contactSessions = new ContactSessionStore();
        contactSessions.TrySetNewerSession(new ContactSessionContext(ReceiverId, 1, "World 1", 1, "receiver"));
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1", "instance-a", 1));

        var receiver = new CharacterDto { MasterId = ReceiverId, DisplayName = "Receiver" };
        var sender = new CharacterDto { MasterId = SenderId, DisplayName = "Sender" };
        _characterService.Setup(x => x.FindCharacterByDisplayName("Receiver")).ReturnsAsync(receiver);
        _characterService.Setup(x => x.FindCharacterByIdAsync(SenderId)).ReturnsAsync(sender);
        _mapper.Setup(x => x.Map<ContactMessageNotification.SenderDto>(sender)).Returns(new ContactMessageNotification.SenderDto { MasterId = SenderId, DisplayName = "Sender" });

        _context = new Mock<ConsumeContext<AddContactMessageRequest>>();
        _context.Setup(x => x.Message).Returns(new AddContactMessageRequest
        {
            MasterId = SenderId, ContactDisplayName = "Receiver", MessageLength = 1, MessagePayload = [1]
        });

        _consumer = new AddContactMessageConsumer(_characterService.Object, _contactService.Object, contactSessions, worldSessions, _mapper.Object);
    }

    [TestMethod]
    public async Task Consume_AvailabilityOff_BlocksEvenForFriend()
    {
        SetSettings(off: true);
        SetRecipientFriendOfSender(true);

        await Assert.ThrowsExactlyAsync<NotFoundException>(() => _consumer.Consume(_context.Object));
        VerifyNotDelivered();
    }

    [TestMethod]
    public async Task Consume_AvailabilityFriends_BlocksWhenSenderNotInRecipientFriends()
    {
        SetSettings(friends: true);
        SetRecipientFriendOfSender(false); // reverse-only friendship: sender may have friended receiver, receiver has not

        await Assert.ThrowsExactlyAsync<NotFoundException>(() => _consumer.Consume(_context.Object));
        VerifyNotDelivered();
    }

    [TestMethod]
    public async Task Consume_AvailabilityFriends_AllowsRecipientFriend()
    {
        SetSettings(friends: true);
        SetRecipientFriendOfSender(true);

        await _consumer.Consume(_context.Object);

        VerifyDelivered();
    }

    [TestMethod]
    public async Task Consume_AvailabilityEveryone_Allows()
    {
        SetSettings(everyone: true);

        await _consumer.Consume(_context.Object);

        VerifyDelivered();
    }

    [TestMethod]
    public async Task Consume_IgnoredSender_BlocksEvenWhenEveryone()
    {
        SetSettings(everyone: true);
        _contactService.Setup(x => x.FindIgnoreByIdAsync(ReceiverId, SenderId))
            .ReturnsAsync(new ContactDto { MasterId = SenderId, DisplayName = "Sender" });

        await Assert.ThrowsExactlyAsync<NotAllowedException>(() => _consumer.Consume(_context.Object));
        VerifyNotDelivered();
    }

    private void SetSettings(bool off = false, bool friends = false, bool everyone = false) =>
        _contactService.Setup(x => x.FindContactSettingsAsync(ReceiverId)).ReturnsAsync(new ContactSettingsDto
        {
            Availability = new ContactSettingsDto.AvailabilitySettingsDto { Off = off, Friends = friends, Everyone = everyone }
        });

    private void SetRecipientFriendOfSender(bool isFriend) =>
        _contactService.Setup(x => x.FindFriendByIdAsync(ReceiverId, SenderId))
            .ReturnsAsync(isFriend ? new ContactDto { MasterId = SenderId, DisplayName = "Sender" } : null);

    private void VerifyDelivered() =>
        _context.Verify(x => x.Publish(It.IsAny<ContactMessageNotification>(), It.IsAny<CancellationToken>()), Times.Once);

    private void VerifyNotDelivered() =>
        _context.Verify(x => x.Publish(It.IsAny<ContactMessageNotification>(), It.IsAny<CancellationToken>()), Times.Never);
}
