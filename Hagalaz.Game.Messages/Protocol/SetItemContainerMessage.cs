using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetItemContainerMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required bool Split { get; init; }
        public required IReadOnlyDictionary<int, IItemBase?> Items { get; init; } = default!;
    }
}
