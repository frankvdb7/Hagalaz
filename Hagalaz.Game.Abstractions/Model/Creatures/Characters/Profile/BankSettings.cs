namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Profile
{
    public record BankSettings
    {
        public required int OptionX { get; init; }
        public required int[] Tabs { get; init; }
    }
}
