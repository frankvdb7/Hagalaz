namespace Hagalaz.Game.Abstractions.Logic.Hydrations
{
    public interface IHydratable<in THydration>
    {
        void Hydrate(THydration hydration);
    }
}
