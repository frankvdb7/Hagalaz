using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record GameObjectFindAll
    {
        public required ILocation Location { get; init; }
        public int Id { get; init; }
    }
}