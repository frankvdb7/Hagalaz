namespace Hagalaz.Game.Abstractions.Model;

/// <summary>
/// Identifies one live entity independently of its protocol/index slot.
/// </summary>
public readonly record struct EntityHandle(int Slot, uint Generation);
