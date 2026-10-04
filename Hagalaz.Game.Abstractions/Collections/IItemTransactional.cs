namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Marks an item-domain object that can contribute storage to an item-container transaction.</summary>
/// <remarks>This capability is provided by item-domain implementations. Arbitrary external implementations are not supported by <see cref="ItemContainerTransaction"/>.</remarks>
public interface IItemTransactional { }
