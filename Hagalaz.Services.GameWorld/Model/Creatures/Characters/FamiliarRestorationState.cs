using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters;

/// <summary>
/// Holds familiar persistence data until the familiar NPC can be composed.
/// </summary>
public sealed class FamiliarRestorationState
{
    /// <summary>
    /// Gets the pending familiar NPC identifier, if one was hydrated.
    /// </summary>
    public int? FamiliarId { get; private set; }

    /// <summary>
    /// Gets the pending familiar runtime state.
    /// </summary>
    public HydratedFamiliar? Familiar { get; private set; }

    /// <summary>
    /// Gets the pending familiar inventory.
    /// </summary>
    public IReadOnlyList<HydratedItem>? Inventory { get; private set; }

    /// <summary>
    /// Stores persisted familiar data for later NPC composition.
    /// </summary>
    /// <param name="hydration">The persisted familiar data.</param>
    public void SetFamiliar(HydratedFamiliarDto hydration)
    {
        FamiliarId = hydration.FamiliarId;
        Familiar = new HydratedFamiliar
        {
            TicksRemaining = hydration.TicksRemaining,
            IsUsingSpecialMove = hydration.IsUsingSpecialMove,
            SpecialMovePoints = hydration.SpecialMovePoints,
        };
    }

    /// <summary>
    /// Stores persisted familiar inventory for later NPC composition.
    /// </summary>
    /// <param name="inventory">The persisted familiar inventory.</param>
    public void SetInventory(IReadOnlyList<HydratedItem> inventory) => Inventory = inventory;

    /// <summary>
    /// Clears the pending restoration data after successful composition.
    /// </summary>
    public void Clear()
    {
        FamiliarId = null;
        Familiar = null;
        Inventory = null;
    }
}
