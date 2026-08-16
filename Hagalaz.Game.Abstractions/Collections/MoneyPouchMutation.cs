namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Represents a checked money-pouch operation, including any inventory overflow.
/// </summary>
public sealed class MoneyPouchMutation
{
    private readonly TradeItemMutation? _pouchMutation;
    private readonly TradeItemMutation? _inventoryMutation;

    internal MoneyPouchMutation(
        bool succeeded,
        TradeItemMutation? pouchMutation,
        TradeItemMutation? inventoryMutation)
    {
        Succeeded = succeeded;
        _pouchMutation = pouchMutation;
        _inventoryMutation = inventoryMutation;
    }

    /// <summary>
    /// Gets whether the requested pouch operation was applied completely.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the number of coins still applied to the pouch by this mutation.
    /// </summary>
    public int PouchCount => _pouchMutation?.AppliedCount ?? 0;

    /// <summary>
    /// Gets the number of overflow or underflow coins still applied to the inventory.
    /// </summary>
    public int InventoryCount => _inventoryMutation?.AppliedCount ?? 0;

    /// <summary>
    /// Gets the total number of coins still applied by this mutation.
    /// </summary>
    public int AppliedCount => PouchCount + InventoryCount;

    /// <summary>
    /// Gets whether this mutation still changes the pouch or inventory.
    /// </summary>
    public bool HasChanges => AppliedCount > 0;

    /// <summary>
    /// Attempts to restore the exact pouch and inventory deltas.
    /// </summary>
    public bool TryRollback()
    {
        var inventoryRestored = _inventoryMutation?.TryRollback() ?? true;
        var pouchRestored = _pouchMutation?.TryRollback() ?? true;
        return inventoryRestored & pouchRestored;
    }

    /// <summary>
    /// Creates an empty pouch mutation result.
    /// </summary>
    /// <param name="succeeded">Whether the requested operation succeeded.</param>
    public static MoneyPouchMutation Empty(bool succeeded) => new(succeeded, null, null);
}
