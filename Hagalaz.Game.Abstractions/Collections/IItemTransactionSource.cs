using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections;

internal interface IItemTransactionSource : IItemTransactional
{
    IReadOnlyList<ItemContainerSynchronization> Synchronizations { get; }
}
