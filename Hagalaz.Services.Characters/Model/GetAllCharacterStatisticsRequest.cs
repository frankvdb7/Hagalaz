using Hagalaz.Services.Common.Model;

namespace Hagalaz.Services.Characters.Model
{
    public record GetAllCharacterStatisticsRequest(
        GetAllCharacterStatisticsRequest.SortModel? Sort,
        GetAllCharacterStatisticsRequest.FilterModel? Filter)
    {
        public record SortModel
        {
            public SortType? Experience { get; }
        }

        public record FilterModel
        {
            public required int Page { get; init; }
            //[Range(1, 100)]
            public required int Limit { get; init; }
            public required CharacterStatisticType Type { get; init; }
        } 
    }
}