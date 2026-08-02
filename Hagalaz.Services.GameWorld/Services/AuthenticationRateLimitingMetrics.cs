using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Hagalaz.Game.Configuration;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Metrics for sign-in throttling, separated from the auth outage circuit breaker.
/// </summary>
public sealed class AuthenticationRateLimitingMetrics
{
    public const string MeterName = "Hagalaz.Services.GameWorld.Authentication";

    private static readonly KeyValuePair<string, object?> PartitionTags = new("partition", "remote_ip");
    private static readonly KeyValuePair<string, object?> AdmissionTags = new("partition", "global_admission");
    private static readonly KeyValuePair<string, object?> GlobalTags = new("partition", "global");

    private readonly Counter<long> _partitionRejected;
    private readonly Counter<long> _globalAdmissionRejected;
    private readonly Counter<long> _globalRejected;
    private readonly KeyValuePair<string, object?> _worldTag;

    public AuthenticationRateLimitingMetrics(IMeterFactory meterFactory, IOptions<WorldOptions> worldOptions)
    {
        var meter = meterFactory.Create(MeterName);
        _partitionRejected = meter.CreateCounter<long>("hagalaz.auth.sign_in.rate_limited.partition");
        _globalAdmissionRejected = meter.CreateCounter<long>("hagalaz.auth.sign_in.rate_limited.global_admission");
        _globalRejected = meter.CreateCounter<long>("hagalaz.auth.sign_in.rate_limited.global");
        _worldTag = new KeyValuePair<string, object?>("world", worldOptions.Value.Id);
    }

    public void RecordPartitionRejected() => _partitionRejected.Add(1, PartitionTags, _worldTag);

    public void RecordGlobalAdmissionRejected() => _globalAdmissionRejected.Add(1, AdmissionTags, _worldTag);

    public void RecordGlobalRejected() => _globalRejected.Add(1, GlobalTags, _worldTag);
}
