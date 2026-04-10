using Hagalaz.Services.Common.Model;

namespace Hagalaz.Services.Characters.Model
{
    public record GetAllCharacterStatisticsRequest(
        GetAllCharacterStatisticsRequest.SortModel? Sort,
        GetAllCharacterStatisticsRequest.FilterModel? Filter)
    {
        public record SortModel
        {
            /// <summary>
            /// Gets or sets the experience sort type.
            /// Using init accessor to allow System.Text.Json deserialization while maintaining immutability.
            /// </summary>
            public SortType? Experience { get; init; }
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