namespace Hagalaz.Game.Abstractions.Logic.Dehydrations
{
    public interface IDehydratable<out TDehydration>
    {
        TDehydration Dehydrate();
    }
}
