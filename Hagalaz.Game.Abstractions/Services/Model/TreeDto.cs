namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record TreeDto
    {
        public required int Id { get; init; }
        public required int StumpId { get; init; }
    }
}
