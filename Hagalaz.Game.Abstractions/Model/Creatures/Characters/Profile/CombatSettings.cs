namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Profile
{
    public record CombatSettings
    {
        public int AttackStyleOptionId { get; init; }
        public bool AutoRetaliating { get; init; }
    }
}
