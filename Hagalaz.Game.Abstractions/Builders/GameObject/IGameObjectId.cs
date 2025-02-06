using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameObjectId
    {
        public IGameObjectLocation WithId(int id);
    }
}