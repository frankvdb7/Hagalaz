namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    public interface INpcHandle
    {
        INpc Npc { get; }
        void Unregister();
    }
}