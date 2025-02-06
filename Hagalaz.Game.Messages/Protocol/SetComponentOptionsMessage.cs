using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetComponentOptionsMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int ChildId { get; init; }
        public required int MinimumSlot { get; init; }
        public required int MaximumSlot { get; init; }
        public required int Value { get; init; }
    }
}
