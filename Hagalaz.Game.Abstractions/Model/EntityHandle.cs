namespace Hagalaz.Game.Abstractions.Model;

/// <summary>
/// Identifies one live entity independently of its protocol/index slot.
/// </summary>
public readonly record struct EntityHandle<TEntity>
    where TEntity : class, IEntity
{
    public int Slot { get; }

    public uint Generation { get; }

    internal EntityHandle(int slot, uint generation)
    {
        Slot = slot;
        Generation = generation;
    }
}
