namespace Hagalaz.Services.Characters.Mediator.Queries
{
    public record GetCharacterStatisticsQuery 
    {
        public required uint MasterId { get; init; }
    }
}