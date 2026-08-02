using System;
using System.Threading.RateLimiting;
using Polly;

namespace Hagalaz.Services.GameWorld.Services;

internal static class AuthenticationRateLimiting
{
    internal const int PartitionPermitLimit = 5;
    internal const int PartitionQueueLimit = 10;
    internal const int GlobalPermitLimit = 1_000;

    internal static readonly ResiliencePropertyKey<string> PartitionKey = new("auth-sign-in-partition");

    internal static PartitionedRateLimiter<ResilienceContext> CreatePartitionedLimiter(
        int permitLimit = PartitionPermitLimit,
        int queueLimit = PartitionQueueLimit) =>
        PartitionedRateLimiter.Create<ResilienceContext, string>(context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                context.Properties.GetValue(PartitionKey, "unknown"),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 10,
                    AutoReplenishment = true
                }));

    internal static SlidingWindowRateLimiter CreateGlobalLimiter() => new(new SlidingWindowRateLimiterOptions
    {
        PermitLimit = GlobalPermitLimit,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        Window = TimeSpan.FromMinutes(1),
        SegmentsPerWindow = 10,
        AutoReplenishment = true
    });
}
