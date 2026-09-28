namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Exposes a container's composed storage to shared infrastructure.</summary>
public interface IItemContainerStorageProvider
{
    ItemContainerStorage Storage { get; }
}
