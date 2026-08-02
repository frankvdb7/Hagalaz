using System.Threading.RateLimiting;
using Polly;
using Polly.RateLimiting;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AuthenticationRateLimitingTests
{
    [TestMethod]
    public async Task PartitionedLimiter_ExhaustingOneRemoteIp_DoesNotRejectAnother()
    {
        using var limiter = AuthenticationRateLimiting.CreatePartitionedLimiter(queueLimit: 0);
        var firstClient = CreateContext("ip:192.0.2.10");
        var secondClient = CreateContext("ip:192.0.2.11");
        var firstClientLeases = new List<RateLimitLease>();

        try
        {
            for (var i = 0; i < AuthenticationRateLimiting.PartitionPermitLimit; i++)
            {
                var lease = await limiter.AcquireAsync(firstClient, 1);
                Assert.IsTrue(lease.IsAcquired);
                firstClientLeases.Add(lease);
            }

            var exhaustedLease = await limiter.AcquireAsync(firstClient, 1);
            var unrelatedLease = await limiter.AcquireAsync(secondClient, 1);

            Assert.IsFalse(exhaustedLease.IsAcquired);
            Assert.IsTrue(unrelatedLease.IsAcquired);
            exhaustedLease.Dispose();
            unrelatedLease.Dispose();
        }
        finally
        {
            foreach (var lease in firstClientLeases)
            {
                lease.Dispose();
            }

            ResilienceContextPool.Shared.Return(firstClient);
            ResilienceContextPool.Shared.Return(secondClient);
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PartitionedLimiter_QueuesBurstForOneRemoteIp_WithoutBlockingAnother()
    {
        using var limiter = AuthenticationRateLimiting.CreatePartitionedLimiter();
        var firstClient = CreateContext("ip:192.0.2.20");
        var secondClient = CreateContext("ip:192.0.2.21");
        var firstClientLeases = new List<RateLimitLease>();
        using var cancellationSource = new CancellationTokenSource();

        try
        {
            for (var i = 0; i < AuthenticationRateLimiting.PartitionPermitLimit; i++)
            {
                firstClientLeases.Add(await limiter.AcquireAsync(firstClient, 1));
            }

            var queuedLeaseTask = limiter.AcquireAsync(firstClient, 1, cancellationSource.Token);
            Assert.IsFalse(queuedLeaseTask.IsCompleted);

            var unrelatedLease = await limiter.AcquireAsync(secondClient, 1);
            Assert.IsTrue(unrelatedLease.IsAcquired);

            cancellationSource.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => queuedLeaseTask.AsTask());

            unrelatedLease.Dispose();
        }
        finally
        {
            foreach (var lease in firstClientLeases)
            {
                lease.Dispose();
            }

            ResilienceContextPool.Shared.Return(firstClient);
            ResilienceContextPool.Shared.Return(secondClient);
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task CombinedLimiter_PerIpRejection_DoesNotConsumeGlobalWindowPermit()
    {
        using var globalAdmissionLimiter = AuthenticationRateLimiting.CreateGlobalAdmissionLimiter(permitLimit: 1);
        using var partitionedLimiter = AuthenticationRateLimiting.CreatePartitionedLimiter(permitLimit: 1, queueLimit: 0);
        using var globalLimiter = AuthenticationRateLimiting.CreateGlobalLimiter(permitLimit: 2);
        var pipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => globalAdmissionLimiter.AcquireAsync(1, args.Context.CancellationToken)
            })
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => partitionedLimiter.AcquireAsync(args.Context, 1, args.Context.CancellationToken)
            })
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => globalLimiter.AcquireAsync(1, args.Context.CancellationToken)
            })
            .Build();

        var firstClient = CreateContext("ip:192.0.2.30");
        var secondClient = CreateContext("ip:192.0.2.31");
        var thirdClient = CreateContext("ip:192.0.2.32");

        try
        {
            await ExecuteAsync(pipeline, firstClient);
            await Assert.ThrowsAsync<RateLimiterRejectedException>(() => ExecuteAsync(pipeline, firstClient));
            await ExecuteAsync(pipeline, secondClient);
            await Assert.ThrowsAsync<RateLimiterRejectedException>(() => ExecuteAsync(pipeline, thirdClient));
        }
        finally
        {
            ResilienceContextPool.Shared.Return(firstClient);
            ResilienceContextPool.Shared.Return(secondClient);
            ResilienceContextPool.Shared.Return(thirdClient);
        }
    }

    private static Task ExecuteAsync(ResiliencePipeline pipeline, ResilienceContext context) =>
        pipeline.ExecuteAsync(_ => ValueTask.CompletedTask, context).AsTask();

    private static ResilienceContext CreateContext(string partitionKey)
    {
        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(AuthenticationRateLimiting.PartitionKey, partitionKey);
        return context;
    }
}
