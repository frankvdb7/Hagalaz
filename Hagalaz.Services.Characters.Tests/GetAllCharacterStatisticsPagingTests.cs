using System.ComponentModel.DataAnnotations;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Mediator.Consumers;
using Hagalaz.Services.Characters.Mediator.Queries;
using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Common.Model;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Characters.Tests;

[TestClass]
public sealed class GetAllCharacterStatisticsPagingTests
{
    private const int SeededCharacters = 150;

    [TestMethod]
    [DataRow(1, 10, true)]
    [DataRow(1, 100, true)]
    [DataRow(10_000_000, 100, true)]
    [DataRow(0, 10, false)]
    [DataRow(-1, 10, false)]
    [DataRow(1, 0, false)]
    [DataRow(1, -5, false)]
    [DataRow(1, 101, false)]
    [DataRow(1, int.MaxValue, false)]
    [DataRow(int.MaxValue, 100, false)]
    [DataRow(10_000_001, 100, false)]
    public void FilterModel_ValidatesPageAndLimit(int page, int limit, bool expectedValid)
    {
        var filter = new GetAllCharacterStatisticsRequest.FilterModel
        {
            Page = page,
            Limit = limit,
            Type = CharacterStatisticType.Overall
        };

        var valid = Validator.TryValidateObject(filter, new ValidationContext(filter), null, validateAllProperties: true);

        Assert.AreEqual(expectedValid, valid);
    }

    [TestMethod]
    public void FilterModel_MaxPageTimesMaxLimit_DoesNotOverflowSkip()
    {
        var skip = checked(GetAllCharacterStatisticsRequest.FilterModel.MaxLimit *
                           (GetAllCharacterStatisticsRequest.FilterModel.MaxPage - 1));

        Assert.IsTrue(skip < int.MaxValue);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Consume_MissingPaging_ReturnsAtMostMaxLimitResults()
    {
        var result = await QueryAsync(new GetAllCharacterStatisticsQuery());

        Assert.HasCount(GetAllCharacterStatisticsRequest.FilterModel.MaxLimit, result.Results);
        Assert.IsNotNull(result.MetaData);
        Assert.AreEqual(SeededCharacters, result.MetaData.Total);
        Assert.AreEqual(1, result.MetaData.Page);
        Assert.AreEqual(GetAllCharacterStatisticsRequest.FilterModel.MaxLimit, result.MetaData.Limit);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Consume_FilterWithoutPaging_ReturnsAtMostMaxLimitResults()
    {
        var result = await QueryAsync(new GetAllCharacterStatisticsQuery
        {
            Filter = new GetAllCharacterStatisticsQuery.FilterModel(CharacterStatisticType.Attack)
        });

        Assert.HasCount(GetAllCharacterStatisticsRequest.FilterModel.MaxLimit, result.Results);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Consume_NormalPages_ReturnRequestedSlice()
    {
        var first = await QueryAsync(new GetAllCharacterStatisticsQuery { Paging = new PagingModel(1, 40) });
        var last = await QueryAsync(new GetAllCharacterStatisticsQuery { Paging = new PagingModel(2, 100) });
        var beyond = await QueryAsync(new GetAllCharacterStatisticsQuery { Paging = new PagingModel(3, 100) });

        Assert.HasCount(40, first.Results);
        Assert.HasCount(SeededCharacters - 100, last.Results);
        Assert.IsEmpty(beyond.Results);
    }

    [TestMethod]
    [DataRow(CharacterStatisticType.Overall)]
    [DataRow(CharacterStatisticType.Attack)]
    [DataRow(CharacterStatisticType.Woodcutting)]
    [Timeout(60000)]
    public async Task Consume_TiedExperience_PagesDoNotOverlapAndCoverAllRows(CharacterStatisticType type)
    {
        // Every seeded character has identical (zero) experience, so only the MasterId tiebreaker defines order.
        var filter = new GetAllCharacterStatisticsQuery.FilterModel(type);
        var first = await QueryAsync(new GetAllCharacterStatisticsQuery { Filter = filter, Paging = new PagingModel(1, 100) });
        var second = await QueryAsync(new GetAllCharacterStatisticsQuery { Filter = filter, Paging = new PagingModel(2, 100) });

        var page1 = first.Results.Select(r => r.DisplayName).ToList();
        var page2 = second.Results.Select(r => r.DisplayName).ToList();

        Assert.HasCount(100, page1);
        Assert.HasCount(SeededCharacters - 100, page2);
        Assert.IsEmpty(page1.Intersect(page2));
        Assert.HasCount(SeededCharacters, page1.Concat(page2).Distinct().ToList());
    }

    private static async Task<GetAllCharacterStatisticsResult> QueryAsync(GetAllCharacterStatisticsQuery query)
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seedContext = CreateContext(databaseName))
        {
            for (uint id = 1; id <= SeededCharacters; id++)
            {
                seedContext.Characters.Add(new Character
                {
                    Id = id,
                    UserName = "user" + id,
                    NormalizedUserName = "USER" + id,
                    DisplayName = "User" + id,
                    RegisterIp = "127.0.0.1"
                });
                seedContext.CharactersStatistics.Add(new CharactersStatistic
                {
                    MasterId = id,
                    XpCounters = "",
                    TrackedXpCounters = "",
                    EnabledXpCounters = "",
                    TargetSkillLevels = "",
                    TargetSkillExperiences = ""
                });
            }

            await seedContext.SaveChangesAsync();
        }

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<GetAllCharacterStatisticsQueryConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();
        var response = await harness.GetRequestClient<GetAllCharacterStatisticsQuery>()
            .GetResponse<GetAllCharacterStatisticsResult>(query);
        await harness.Stop();
        return response.Message;
    }

    private static HagalazDbContext CreateContext(string databaseName) => new(
        new DbContextOptionsBuilder<HagalazDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);
}
