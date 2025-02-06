using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record ContactSettingsChangedMessage(uint MasterId, ContactDto MasterContact);
}