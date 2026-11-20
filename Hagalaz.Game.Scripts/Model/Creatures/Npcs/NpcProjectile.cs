namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    ///     Looks of an NPC projectile: its graphic and launch timing/geometry. Duration comes from the attack's delay.
    /// </summary>
    public readonly record struct NpcProjectile(int GraphicId, int LaunchDelay, int FromHeight = 35, int ToHeight = 35, int Angle = 180);
}
