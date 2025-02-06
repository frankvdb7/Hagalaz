namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    public record BarrowsDto
    {
        public int KillCount { get; init; }
        public int CryptStartIndex { get; init; }
        public int TunnelIndex { get; init; }
        public bool LootedChest { get; init; }

        public bool AhrimKilled { get; init; }
        public bool DharokKilled { get; init; }
        public bool GuthanKilled { get; init; }
        public bool KarilKilled { get; init; }
        public bool ToragKilled { get; init; }
        public bool VeracKilled { get; init; }
        public bool AkriseaKilled { get; init; }
    }
}
