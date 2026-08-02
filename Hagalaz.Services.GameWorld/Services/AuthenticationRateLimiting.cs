using System;
using System.Threading.RateLimiting;
using Polly;

namespace Hagalaz.Services.GameWorld.Services;

internal static class AuthenticationRateLimiting
{
    internal const int PartitionPermitLimit = 5;
    internal const int PartitionQueueLimit = 10;
    internal const int GlobalAdmissionPermitLimit = 1_000;
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

    internal static ConcurrencyLimiter CreateGlobalAdmissionLimiter(
        int permitLimit = GlobalAdmissionPermitLimit) => new(new ConcurrencyLimiterOptions
    {
        PermitLimit = permitLimit,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

    internal static SlidingWindowRateLimiter CreateGlobalLimiter(
        int permitLimit = GlobalPermitLimit) => new(new SlidingWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        Window = TimeSpan.FromMinutes(1),
        SegmentsPerWindow = 10,
        AutoReplenishment = true
    });
}
