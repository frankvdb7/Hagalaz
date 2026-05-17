using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;
using Moq;
using ContactDto = Hagalaz.Services.Contacts.Services.Model.ContactDto;
using MsgContactDto = Hagalaz.Contacts.Messages.Model.ContactDto;
using ContactSettingsDto = Hagalaz.Services.Contacts.Services.Model.ContactSettingsDto;

namespace Hagalaz.Services.Contacts.Tests
{
    [TestClass]
    public class AddRemoveContactConsumerTests
    {
        private Mock<IContactService> _contactServiceMock = null!;
        private Mock<ICharacterService> _characterServiceMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private ContactSessionStore _contactSessions = null!;
        private WorldSessionStore _worldSessions = null!;
        private AddRemoveContactConsumer _consumer = null!;

        [TestInitialize]
        public void Initialize()
        {
            _contactServiceMock = new Mock<IContactService>();
            _characterServiceMock = new Mock<ICharacterService>();
            _mapperMock = new Mock<IMapper>();
            _contactSessions = new ContactSessionStore();
            _worldSessions = new WorldSessionStore();
            _consumer = new AddRemoveContactConsumer(_contactSessions, _worldSessions, _contactServiceMock.Object, _characterServiceMock.Object, _mapperMock.Object);
        }

        [TestMethod]
        public async Task Consume_AddContactRequest_PublishesCorrectWorldIdForMaster()
        {
            // Arrange
            uint masterId = 100;
            uint contactId = 200;
            string contactName = "ContactUser";
            int masterWorldId = 1;
            int contactWorldId = 2;

            var masterCharacter = new Hagalaz.Services.Contacts.Services.Model.CharacterDto { MasterId = masterId, DisplayName = "Master" };
            var contactCharacter = new Hagalaz.Services.Contacts.Services.Model.CharacterDto { MasterId = contactId, DisplayName = contactName };

            _characterServiceMock.Setup(x => x.FindCharacterByIdAsync(masterId)).ReturnsAsync(masterCharacter);
            _characterServiceMock.Setup(x => x.FindCharacterByDisplayName(contactName)).ReturnsAsync(contactCharacter);
            _contactServiceMock.Setup(x => x.AddContactAsync(masterId, contactId, false)).ReturnsAsync(Hagalaz.Services.Common.Model.Result.Success);

            _contactSessions.TryAdd(masterId, new ContactSessionContext(masterId, masterWorldId, "World 1"));
            _contactSessions.TryAdd(contactId, new ContactSessionContext(contactId, contactWorldId, "World 2"));
            _worldSessions.TryAdd(masterWorldId, new WorldSessionContext(masterWorldId, "World 1"));
            _worldSessions.TryAdd(contactWorldId, new WorldSessionContext(contactWorldId, "World 2"));

            _mapperMock.Setup(m => m.Map<MsgContactDto>(contactCharacter)).Returns(new MsgContactDto { MasterId = contactId, DisplayName = contactName });
            _mapperMock.Setup(m => m.Map<MsgContactDto>(masterCharacter)).Returns(new MsgContactDto { MasterId = masterId, DisplayName = "Master" });

            var contextMock = new Mock<ConsumeContext<AddContactRequest>>();
            contextMock.Setup(x => x.Message).Returns(new AddContactRequest { MasterId = masterId, ContactDisplayName = contactName, Ignore = false });

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            contextMock.Verify(x => x.Publish(It.Is<ContactAddedMessage>(m =>
                m.Master.MasterId == masterId &&
                m.Master.WorldId == masterWorldId &&
                m.Contact.MasterId == contactId &&
                m.Contact.WorldId == contactWorldId
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Consume_AddContactRequest_SetsCorrectAvailabilityFromSettings()
        {
            // Arrange
            uint masterId = 100;
            uint contactId = 200;
            string contactName = "ContactUser";

            var masterCharacter = new Hagalaz.Services.Contacts.Services.Model.CharacterDto { MasterId = masterId, DisplayName = "Master" };
            var contactCharacter = new Hagalaz.Services.Contacts.Services.Model.CharacterDto { MasterId = contactId, DisplayName = contactName };

            _characterServiceMock.Setup(x => x.FindCharacterByIdAsync(masterId)).ReturnsAsync(masterCharacter);
            _characterServiceMock.Setup(x => x.FindCharacterByDisplayName(contactName)).ReturnsAsync(contactCharacter);
            _contactServiceMock.Setup(x => x.AddContactAsync(masterId, contactId, false)).ReturnsAsync(Hagalaz.Services.Common.Model.Result.Success);

            // Contact has Friends-only availability
            var contactFriendOfMaster = new ContactDto
            {
                MasterId = contactId,
                DisplayName = contactName,
                Settings = new ContactSettingsDto { Availability = new ContactSettingsDto.AvailabilitySettingsDto { Friends = true, Everyone = false, Off = false } }
            };
            _contactServiceMock.Setup(x => x.FindFriendByIdAsync(contactId, masterId)).ReturnsAsync(contactFriendOfMaster);

            _mapperMock.Setup(m => m.Map<MsgContactDto>(contactCharacter)).Returns(new MsgContactDto { MasterId = contactId, DisplayName = contactName });
            _mapperMock.Setup(m => m.Map<MsgContactDto>(masterCharacter)).Returns(new MsgContactDto { MasterId = masterId, DisplayName = "Master" });

            var contextMock = new Mock<ConsumeContext<AddContactRequest>>();
            contextMock.Setup(x => x.Message).Returns(new AddContactRequest { MasterId = masterId, ContactDisplayName = contactName, Ignore = false });

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            contextMock.Verify(x => x.Publish(It.Is<ContactAddedMessage>(m =>
                m.Contact.Settings != null && m.Contact.Settings.Availability == ContactAvailability.Friends
            ), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
