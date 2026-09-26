using System.Collections;
using System.Linq.Expressions;
using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Network.Consumers;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldStatusRequestConsumerTests
{
    [TestMethod]
    public async Task Consume_WithResponseAddress_RespondsWithoutPublishingDuplicateOnlineMessage()
    {
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var context = CreateContext(new Uri("loopback://response"));
        var consumer = CreateConsumer(publishEndpoint);

        await consumer.Consume(context);

        await context.Received(1).RespondAsync(Arg.Any<WorldOnlineMessage>());
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(default(WorldOnlineMessage)!, default);
    }

    [TestMethod]
    public async Task Consume_WithoutResponseAddress_PublishesOnlineMessageWithoutResponding()
    {
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var context = CreateContext(null);
        var consumer = CreateConsumer(publishEndpoint);

        await consumer.Consume(context);

        await publishEndpoint.Received(1).Publish(Arg.Any<WorldOnlineMessage>(), Arg.Any<CancellationToken>());
        await context.DidNotReceiveWithAnyArgs().RespondAsync(default(WorldOnlineMessage)!);
    }

    private static WorldStatusRequestConsumer CreateConsumer(IPublishEndpoint publishEndpoint)
    {
        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.FindWorldById(1).Returns(new TestAsyncEnumerable<World>(new[]
        {
            new World
            {
                Id = 1,
                Name = "World 1",
                MembersOnly = 1,
                QuickChatAllowed = 1,
                HighRisk = 0,
                LootShareAllowed = 1,
                Highlight = 0,
                Region = "Local",
                Country = 0
            }
        }));

        var characterService = Substitute.For<ICharacterService>();
#pragma warning disable CA2012
        characterService.CountAsync().Returns(new ValueTask<int>(3));
#pragma warning restore CA2012
        var mapper = Substitute.For<IMapper>();
        mapper.Map<WorldOnlineMessage.WorldSettings>(Arg.Any<World>()).Returns(new WorldOnlineMessage.WorldSettings
        {
            IsMembersOnly = true,
            IsQuickChatEnabled = true,
            IsPvP = false,
            IsLootShareEnabled = true,
            IsHighLighted = false
        });
        mapper.Map<WorldOnlineMessage.WorldLocation>(Arg.Any<World>()).Returns(new WorldOnlineMessage.WorldLocation
        {
            Name = "Local",
            Flag = 0
        });

        return new WorldStatusRequestConsumer(
            Options.Create(new WorldOptions
            {
                Id = 1,
                Name = "World 1",
                AdvertisedEndpoint = new WorldEndpointOptions { Host = "127.0.0.1", Port = 443 },
                RegistrationLeaseDuration = TimeSpan.FromMinutes(1)
            }),
            characterService,
            worldRepository,
            mapper,
            new WorldInstanceIdentity(),
            publishEndpoint);
    }

    private static ConsumeContext<WorldStatusRequest> CreateContext(Uri? responseAddress)
    {
        var context = Substitute.For<ConsumeContext<WorldStatusRequest>>();
        context.Message.Returns(new WorldStatusRequest());
        context.ResponseAddress.Returns(responseAddress);
        context.CancellationToken.Returns(CancellationToken.None);
        context.RespondAsync(Arg.Any<WorldOnlineMessage>()).Returns(Task.CompletedTask);
        return context;
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments()[0];
            var queryType = typeof(TestAsyncEnumerable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryType, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = inner.Execute(expression);
            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [executionResult])!;
        }
    }
}
