using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class AddContactReceiverMessage : RaidoMessage
    {
        public required long Id { get; init; }
        public required string SenderDisplayName { get; init; }
        public string? SenderPreviousDisplayName { get; init; }
        public required int SenderRights { get; init; }
        public required int MessageLength { get; init; }
        public required byte[] MessagePayload { get; init; }
    }
}