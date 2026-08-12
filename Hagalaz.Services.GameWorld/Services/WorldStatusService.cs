using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class WorldStatusService : BackgroundService
{
    private readonly IBus _publishEndpoint;
    private readonly IGameMediator _mediator;
    private readonly IOptions<WorldOptions> _worldOptions;
    private readonly ILogger<WorldStatusService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly WorldInstanceIdentity _identity;
    private readonly WorldRegistrationStore _registrations;
    private readonly WorldLifecycleState _lifecycle;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;
    private int _publishedOnline;

    public WorldStatusService(
        IBus publishEndpoint,
        IGameMediator mediator,
        IOptions<WorldOptions> worldOptions,
        ILogger<WorldStatusService> logger,
        IHostApplicationLifetime applicationLifetime,
        WorldInstanceIdentity identity,
        WorldRegistrationStore registrations,
        WorldLifecycleState lifecycle,
        IServiceScopeFactory scopeFactory,
        IMapper mapper)
    {
        _publishEndpoint = publishEndpoint;
        _mediator = mediator;
        _worldOptions = worldOptions;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _identity = identity;
        _registrations = registrations;
        _lifecycle = lifecycle;
        _scopeFactory = scopeFactory;
        _mapper = mapper;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _applicationLifetime.ApplicationStarted.Register(_lifecycle.MarkApplicationStarted);
        _applicationLifetime.ApplicationStopping.Register(_lifecycle.MarkStopping);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialized = await _lifecycle.WaitForInitializationAsync(stoppingToken);
        await _lifecycle.WaitForApplicationStartedAsync(stoppingToken);
        if (!initialized)
        {
            _logger.LogError("World startup initialization failed; registration will not begin.");
            return;
        }

        var discoveryRequested = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var onlineMessage = await _mediator.GetResponseAsync<WorldStatusRequest, WorldOnlineMessage>(new WorldStatusRequest());
                _registrations.ObserveOnline(onlineMessage);
                await _publishEndpoint.Publish(onlineMessage, stoppingToken);
                Interlocked.Exchange(ref _publishedOnline, 1);
                _lifecycle.MarkRegistrationSucceeded();

                if (!discoveryRequested)
                {
                    try
                    {
                        await _publishEndpoint.Publish(new WorldStatusRequest(), stoppingToken);
                        discoveryRequested = true;
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        _logger.LogWarning(exception, "World registration succeeded but discovery reconstruction could not be requested.");
                    }
                }

                await Task.Delay(_worldOptions.Value.RegistrationRenewalInterval, stoppingToken);
                await ReconcileExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _lifecycle.MarkRegistrationFailed();
                _logger.LogError(exception, "World registration publication failed; readiness has been removed.");
                try
                {
                    await Task.Delay(_worldOptions.Value.RegistrationRetryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _lifecycle.MarkStopping();
        await base.StopAsync(cancellationToken);

        if (Volatile.Read(ref _publishedOnline) == 0)
        {
            return;
        }

        try
        {
            var options = _worldOptions.Value;
            await _publishEndpoint.Publish(
                new WorldOfflineMessage(options.Id, _identity.InstanceId, _identity.Generation),
                cancellationToken);
            _logger.LogInformation("{Name} stopped successfully", nameof(WorldStatusService));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while publishing {Type}", nameof(WorldOfflineMessage));
        }
    }

    private async Task ReconcileExpiredAsync(CancellationToken cancellationToken)
    {
        var updates = _registrations.Expire();
        if (updates.Count == 0)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var worldInfoService = scope.ServiceProvider.GetRequiredService<IWorldInfoService>();
        foreach (var update in updates)
        {
            if (update.ActiveMessage == null)
            {
                await worldInfoService.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(update.WorldId, 0, false));
                continue;
            }

            await worldInfoService.AddOrUpdateWorldInfoAsync(_mapper.Map<Model.WorldInfo>(update.ActiveMessage));
            await worldInfoService.UpdateWorldCharacterInfoAsync(
                new WorldCharacterInfo(update.WorldId, update.ActiveMessage.CharacterCount, true));
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
