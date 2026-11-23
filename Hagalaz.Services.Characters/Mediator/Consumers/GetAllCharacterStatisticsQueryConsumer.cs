using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Data.Entities;
using Hagalaz.Data.Extensions;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Mediator.Queries;
using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Common.Model;

namespace Hagalaz.Services.Characters.Mediator.Consumers
{
    public class GetAllCharacterStatisticsQueryConsumer : IConsumer<GetAllCharacterStatisticsQuery>
    {
        private readonly ICharacterUnitOfWork _characterUnitOfWork;
        private readonly IMapper _mapper;

        public GetAllCharacterStatisticsQueryConsumer(ICharacterUnitOfWork characterUnitOfWork, IMapper mapper)
        {
            _characterUnitOfWork = characterUnitOfWork;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<GetAllCharacterStatisticsQuery> context)
        {
            var message = context.Message;
            var total = await _characterUnitOfWork.CharacterStatisticsRepository.CountAsync();
            var statsQuery = _characterUnitOfWork.CharacterStatisticsRepository.FindAll().AsNoTracking();
            IQueryable<CharacterStatisticCollectionDto> dtoQuery;
            var sort = message.Sort;
            var paging = message.Paging ?? new PagingModel(1, GetAllCharacterStatisticsRequest.FilterModel.MaxLimit);
            var filter = message.Filter;
            if (filter != null)
            {
                dtoQuery = FilterByType(statsQuery, filter.Type);
            }
            else
            {
                dtoQuery = _mapper.ProjectTo<CharacterStatisticCollectionDto>(statsQuery.OrderBy(stats => stats.MasterId));
            }

            if (sort?.Experience != null)
            {
                //dtoQuery = sort.Experience.Value == SortType.Asc ? dtoQuery.OrderBy(s => s.Statistics.Count) : query.OrderByDescending(s => s.AgilityExp);
            }

            dtoQuery = dtoQuery.Paging(paging.Page, paging.Limit);

            var dtos = await dtoQuery.ToListAsync(context.CancellationToken);

            await context.RespondAsync(new GetAllCharacterStatisticsResult
            {
                Results = dtos,
                MetaData = new PagingMetaDataModel(total, paging.Page, paging.Limit)
            });
        }

        private IQueryable<CharacterStatisticCollectionDto> FilterByType(IQueryable<CharactersStatistic> statsQuery, CharacterStatisticType type) =>
            type switch
            {
                CharacterStatisticType.Overall => _mapper.ProjectTo<CharactersStatisticsDto>(statsQuery)
                    .OrderByDescending(dto => dto.AgilityExp + dto.AttackExp + dto.ConstitutionExp + dto.ConstructionExp + dto.CookingExp + dto.CraftingExp +
                                              dto.DefenceExp + dto.DungeoneeringExp + dto.FarmingExp + dto.FiremakingExp + dto.FishingExp +
                                              dto.FletchingExp + dto.HerbloreExp + dto.HunterExp + dto.MagicExp + dto.MiningExp + dto.PrayerExp + dto.RangeExp +
                                              dto.RunecraftingExp + dto.SlayerExp + dto.SmithingExp + dto.StrengthExp + dto.SummoningExp + dto.ThievingExp +
                                              dto.WoodcuttingExp)
                    .ThenBy(dto => dto.MasterId)
                    .Select(dto => new CharacterStatisticCollectionDto
                    {
                        DisplayName = dto.DisplayName,
                        Statistics = new[]
                        {
                                new CharacterStatisticDetailDto { Type = CharacterStatisticType.Overall, Level = dto.OverallLevel, Experience = dto.OverallExperience }
                        }
                    }),
                CharacterStatisticType.Agility => statsQuery.OrderByDescending(stats => stats.AgilityExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.AgilityLevel, Experience = stats.AgilityExp }
                        }
                    }),
                CharacterStatisticType.Attack => statsQuery.OrderByDescending(stats => stats.AttackExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.AttackLevel, Experience = stats.AttackExp }
                        }
                    }),
                CharacterStatisticType.Constitution => statsQuery.OrderByDescending(stats => stats.ConstitutionExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.ConstitutionLevel, Experience = stats.ConstitutionExp }
                        }
                    }),
                CharacterStatisticType.Construction => statsQuery.OrderByDescending(stats => stats.ConstructionExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.ConstructionLevel, Experience = stats.ConstructionExp }
                        }
                    }),
                CharacterStatisticType.Cooking => statsQuery.OrderByDescending(stats => stats.CookingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.CookingLevel, Experience = stats.CookingExp }
                        }
                    }),
                CharacterStatisticType.Crafting => statsQuery.OrderByDescending(stats => stats.CraftingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.CraftingLevel, Experience = stats.CraftingExp }
                        }
                    }),
                CharacterStatisticType.Defence => statsQuery.OrderByDescending(stats => stats.DefenceExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.DefenceLevel, Experience = stats.DefenceExp }
                        }
                    }),
                CharacterStatisticType.Dungeoneering => statsQuery.OrderByDescending(stats => stats.DungeoneeringExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.DungeoneeringLevel, Experience = stats.DungeoneeringExp }
                        }
                    }),
                CharacterStatisticType.Farming => statsQuery.OrderByDescending(stats => stats.FarmingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.FarmingLevel, Experience = stats.FarmingExp }
                        }
                    }),
                CharacterStatisticType.Firemaking => statsQuery.OrderByDescending(stats => stats.FiremakingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.FiremakingLevel, Experience = stats.FiremakingExp }
                        }
                    }),
                CharacterStatisticType.Fishing => statsQuery.OrderByDescending(stats => stats.FishingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.FishingLevel, Experience = stats.FishingExp }
                        }
                    }),
                CharacterStatisticType.Fletching => statsQuery.OrderByDescending(stats => stats.FletchingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.FletchingLevel, Experience = stats.FletchingExp }
                        }
                    }),
                CharacterStatisticType.Herblore => statsQuery.OrderByDescending(stats => stats.HerbloreExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.HerbloreLevel, Experience = stats.HerbloreExp }
                        }
                    }),
                CharacterStatisticType.Hunter => statsQuery.OrderByDescending(stats => stats.HunterExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.HunterLevel, Experience = stats.HunterExp }
                        }
                    }),
                CharacterStatisticType.Magic => statsQuery.OrderByDescending(stats => stats.MagicExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.MagicLevel, Experience = stats.MagicExp }
                        }
                    }),
                CharacterStatisticType.Mining => statsQuery.OrderByDescending(stats => stats.MiningExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.MiningLevel, Experience = stats.MiningExp }
                        }
                    }),
                CharacterStatisticType.Prayer => statsQuery.OrderByDescending(stats => stats.PrayerExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.PrayerLevel, Experience = stats.PrayerExp }
                        }
                    }),
                CharacterStatisticType.Range => statsQuery.OrderByDescending(stats => stats.RangeExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.RangeLevel, Experience = stats.RangeExp }
                        }
                    }),
                CharacterStatisticType.Runecrafting => statsQuery.OrderByDescending(stats => stats.RunecraftingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.RunecraftingLevel, Experience = stats.RunecraftingExp }
                        }
                    }),
                CharacterStatisticType.Slayer => statsQuery.OrderByDescending(stats => stats.SlayerExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.SlayerLevel, Experience = stats.SlayerExp }
                        }
                    }),
                CharacterStatisticType.Smithing => statsQuery.OrderByDescending(stats => stats.SmithingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.SmithingLevel, Experience = stats.SmithingExp }
                        }
                    }),
                CharacterStatisticType.Strength => statsQuery.OrderByDescending(stats => stats.StrengthExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.StrengthLevel, Experience = stats.StrengthExp }
                        }
                    }),
                CharacterStatisticType.Summoning => statsQuery.OrderByDescending(stats => stats.SummoningExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.SummoningLevel, Experience = stats.SummoningExp }
                        }
                    }),
                CharacterStatisticType.Thieving => statsQuery.OrderByDescending(stats => stats.ThievingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.ThievingLevel, Experience = stats.ThievingExp }
                        }
                    }),
                CharacterStatisticType.Woodcutting => statsQuery.OrderByDescending(stats => stats.WoodcuttingExp).ThenBy(stats => stats.MasterId)
                    .Select(stats => new CharacterStatisticCollectionDto
                    {
                        DisplayName = stats.Master.DisplayName,
                        Statistics = new CharacterStatisticDetailDto[]
                        {
                                new() { Type = type, Level = stats.WoodcuttingLevel, Experience = stats.WoodcuttingExp }
                        }
                    }),
                _ => throw new NotImplementedException("Statistic type not implemented: " + type)
            };
    }
}