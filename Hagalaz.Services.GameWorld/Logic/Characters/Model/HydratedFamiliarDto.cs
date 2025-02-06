namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedFamiliarDto
    {
        public int FamiliarId { get; init; }
        public int SpecialMovePoints { get; init; }
        public bool IsUsingSpecialMove { get; init; }
        public int TicksRemaining { get; init; }
    }
}
