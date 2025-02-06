using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Common.Model;

namespace Hagalaz.Services.Characters.Mediator.Queries
{
    public record GetAllCharacterStatisticsQuery
    {
        public record SortModel(SortType? Experience);

        public record FilterModel(CharacterStatisticType Type);

        public SortModel? Sort { get; init; }
        public PagingModel? Paging { get; init; }
        public FilterModel? Filter { get; init; }
    }
}