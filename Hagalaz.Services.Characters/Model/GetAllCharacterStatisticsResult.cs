using Hagalaz.Services.Common.Model;

namespace Hagalaz.Services.Characters.Model
{
    public record GetAllCharacterStatisticsResult : MultiValueResult<CharacterStatisticCollectionDto>
    {
        public PagingMetaDataModel? MetaData { get; init; }
    }
}