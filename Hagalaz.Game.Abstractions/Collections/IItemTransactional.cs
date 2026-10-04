namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Marks a supported item-domain object that can participate in an item-container transaction.</summary>
/// <remarks>This is a semantic capability exposed by supported item-domain implementations. <see cref="ItemContainerTransaction"/> does not support arbitrary external implementations of this marker.</remarks>
public interface IItemTransactional { }
