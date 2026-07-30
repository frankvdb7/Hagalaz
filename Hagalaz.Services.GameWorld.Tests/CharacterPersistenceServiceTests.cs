using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterPersistenceServiceTests
{
    private readonly TestContext _testContext;

    public CharacterPersistenceServiceTests(TestContext testContext) => _testContext = testContext;

    [TestMethod]
    [Timeout(10000)]
    public async Task PersistAsync_RetriesTransientUpdateFailureAndPersistsSnapshot()
    {
        RetryUpdateConsumer.Attempts = 0;
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.DehydrateAsync(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>())
            .Returns(Task.FromResult(new CharacterModel()));

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(dehydrationService)
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .AddMassTransitTestHarness(x => x.AddConsumer<RetryUpdateConsumer>())
            .AddSingleton<CharacterPersistenceState>()
            .AddSingleton<SnapshotRevisionGenerator>()
            .AddScoped<ICharacterPersistenceService, CharacterPersistenceService>()
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();
        try
        {
            using var scope = provider.CreateScope();
            var character = Substitute.For<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>();
            character.MasterId.Returns(42u);

            await scope.ServiceProvider.GetRequiredService<ICharacterPersistenceService>()
                .PersistAsync(character, force: true, cancellationToken: _testContext.CancellationToken);

            Assert.AreEqual(3, RetryUpdateConsumer.Attempts);
            Assert.IsTrue(await harness.Consumed.Any<UpdateCharacterRequest>());
            Assert.IsNotNull(RetryUpdateConsumer.LastRequest);
            Assert.AreEqual(42u, RetryUpdateConsumer.LastRequest.MasterId);
            Assert.IsGreaterThan(0L, RetryUpdateConsumer.LastRequest.SnapshotRevision);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private sealed class RetryUpdateConsumer : IConsumer<UpdateCharacterRequest>
    {
        public static int Attempts;
        public static UpdateCharacterRequest? LastRequest;

        public async Task Consume(ConsumeContext<UpdateCharacterRequest> context)
        {
            var attempt = Interlocked.Increment(ref Attempts);
            LastRequest = context.Message;
            if (attempt < 3)
            {
                throw new InvalidOperationException("Transient character persistence failure.");
            }

            await context.RespondAsync(new UpdateCharacterResponse(context.Message.CorrelationId, context.Message.MasterId));
        }
    }
}
