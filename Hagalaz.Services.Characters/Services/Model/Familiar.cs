namespace Hagalaz.Services.Characters.Services.Model
{
    public record Familiar
    {
        public int FamiliarId { get; init; }
        public int SpecialMovePoints { get; init; }
        public bool IsUsingSpecialMove { get; init; }
        public int TicksRemaining { get; init; }
    }
}
