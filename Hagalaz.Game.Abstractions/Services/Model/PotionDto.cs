namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for making a potion.
    /// </summary>
    /// <param name="PotionId">The item ID of the finished potion.</param>
    /// <param name="UnfinishedPotionId">The item ID of the unfinished potion.</param>
    /// <param name="RequiredLevel">The required Herblore level to make this potion.</param>
    /// <param name="Experience">The Herblore experience gained for making this potion.</param>
    /// <param name="PrimaryItemIds">An array of item IDs for the primary ingredients.</param>
    /// <param name="SecondaryItemIds">An array of item IDs for the secondary ingredients.</param>
    public record PotionDto(int PotionId, int UnfinishedPotionId, int RequiredLevel, double Experience, int[] PrimaryItemIds, int[] SecondaryItemIds);
}