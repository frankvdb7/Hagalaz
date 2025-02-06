namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record PotionDto(int PotionId, int UnfinishedPotionId, int RequiredLevel, double Experience, int[] PrimaryItemIds, int[] SecondaryItemIds);
}