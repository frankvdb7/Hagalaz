namespace Hagalaz.Game.Abstractions.Logic.Characters.Model
{
    public record HydratedFamiliar
    {
        public int SpecialMovePoints { get; init; }
        public bool IsUsingSpecialMove { get; init; }
        public int TicksRemaining { get; init; }
    }
}
