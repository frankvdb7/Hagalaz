using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record GameObjectUpdate
    {
        public required IGameObject Instance { get; init; }
        public int Id { get; init; }
        public int Rotation { get; init; }
    }
}