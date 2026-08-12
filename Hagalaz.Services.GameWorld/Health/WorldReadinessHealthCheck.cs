using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Health;

public sealed class WorldReadinessHealthCheck : IHealthCheck
{
    private readonly WorldLifecycleState _lifecycle;
    private readonly WorldRegistrationStore _registrations;
    private readonly WorldInstanceIdentity _identity;
    private readonly IOptionsMonitor<WorldOptions> _options;

    public WorldReadinessHealthCheck(
        WorldLifecycleState lifecycle,
        WorldRegistrationStore registrations,
        WorldInstanceIdentity identity,
        IOptionsMonitor<WorldOptions> options)
    {
        _lifecycle = lifecycle;
        _registrations = registrations;
        _identity = identity;
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        WorldOptions options;
        try
        {
            options = _options.CurrentValue;
        }
        catch (OptionsValidationException exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("World configuration is invalid.", exception));
        }

        if (!_lifecycle.IsApplicationStarted || !_lifecycle.IsInitializationCompleted ||
            !_lifecycle.IsRegistrationHealthy || _lifecycle.IsStopping)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("World initialization or registration is incomplete."));
        }

        if (_registrations.HasConflict(options.Id, _identity.InstanceId) ||
            !_registrations.IsLocalGenerationAvailable(options.Id, _identity.InstanceId))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Another live generation owns the world identity."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
