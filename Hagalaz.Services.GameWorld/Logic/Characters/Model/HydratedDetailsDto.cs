namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedDetailsDto
    {
        public int CoordX { get; init; }
        public int CoordY { get; init; }
        public int CoordZ { get; init; }
    }
}
