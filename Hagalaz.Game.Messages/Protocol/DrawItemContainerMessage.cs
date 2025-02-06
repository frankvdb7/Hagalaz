using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawItemContainerMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required bool Split { get; init; }
        public required int Capacity { get; init; }
        public required IReadOnlyList<IItemBase?> Items { get; init; } = default!;
    }
}
