using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameObjectOptional : IGameObjectBuild
    {
        public IGameObjectOptional WithRotation(int rotation);
        public IGameObjectOptional WithShape(ShapeType shapeType);
        public IGameObjectOptional WithScript(IGameObjectScript script);
        public IGameObjectOptional WithScript<TScript>() where TScript : IGameObjectScript;
        public IGameObjectOptional AsStatic();
    }
}