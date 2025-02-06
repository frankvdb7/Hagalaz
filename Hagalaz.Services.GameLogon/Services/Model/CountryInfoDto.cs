namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record CountryInfoDto
    {
        public string Name { get; init; } = null!;
        public int Flag { get; init; }
    }
}