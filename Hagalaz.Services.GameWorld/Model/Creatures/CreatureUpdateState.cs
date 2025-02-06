namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    internal enum CreatureUpdateState
    {
        Initializing,
        Idle,
        ServerUpdate,
        ClientPrepareUpdate,
        ClientUpdate,
        ClientUpdateReset,
        Destroyed
    }
}