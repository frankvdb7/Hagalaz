using System.ComponentModel.DataAnnotations;
using Hagalaz.Services.Common.Model;

namespace Hagalaz.Services.Characters.Model
{
    public record GetAllCharacterStatisticsRequest(
        GetAllCharacterStatisticsRequest.SortModel? Sort,
        GetAllCharacterStatisticsRequest.FilterModel? Filter)
    {
        public record SortModel
        {
            public SortType? Experience { get; init; }
        }

        public record FilterModel
        {
            public const int MaxLimit = 100;

            // MaxPage * MaxLimit must stay below int.MaxValue so Skip(Limit * (Page - 1)) cannot overflow.
            public const int MaxPage = 10_000_000;

            [Range(1, MaxPage)]
            public required int Page { get; init; }

            [Range(1, MaxLimit)]
            public required int Limit { get; init; }
            public required CharacterStatisticType Type { get; init; }
        } 
    }
}