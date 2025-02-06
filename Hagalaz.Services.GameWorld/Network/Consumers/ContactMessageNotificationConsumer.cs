using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Utilities;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class ContactMessageNotificationConsumer : IConsumer<ContactMessageNotification>
    {
        private readonly IGameConnectionService _connectionService;
        private readonly IClientPermissionProvider _clientPermissionProvider;

        public ContactMessageNotificationConsumer(IGameConnectionService connectionService, IClientPermissionProvider clientPermissionProvider)
        {
            _connectionService = connectionService;
            _clientPermissionProvider = clientPermissionProvider;
        }

        public async Task Consume(ConsumeContext<ContactMessageNotification> context)
        {
            var message = context.Message;
            var connection = await _connectionService.FindByMasterId(message.MasterId);
            if (connection == null)
            {
                return;
            }
            await connection.SendMessage(new AddContactReceiverMessage()
            {
                Id = SessionId.NewId(),
                MessageLength = message.MessageLength,
                MessagePayload = message.MessagePayload,
                SenderDisplayName = message.Sender.DisplayName,
                SenderPreviousDisplayName = message.Sender.PreviousDisplayName,
                SenderRights = _clientPermissionProvider.GetClientPermission(message.Sender.Claims?.Select(c => c.Name)?.ToList() ?? [])
            });
        }
    }
}