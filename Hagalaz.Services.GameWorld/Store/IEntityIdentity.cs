namespace Hagalaz.Services.GameWorld.Store;

/// <summary>
/// Allows the entity store to assign an identity without exposing mutation to gameplay code.
/// </summary>
internal interface IEntityIdentity
{
    int HandleSlot { get; }

    uint HandleGeneration { get; }

    void SetHandle(int slot, uint generation);
}
