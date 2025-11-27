using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class CharacterDehydrationWorkerService : BackgroundService
    {
        private readonly ILogger<CharacterDehydrationWorkerService> _logger;
        private readonly IMapper _mapper;
        private readonly IClientFactory _clientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICharacterStore _characterStore;

        public CharacterDehydrationWorkerService(ILogger<CharacterDehydrationWorkerService> logger,
                                            IMapper mapper,
                                            IClientFactory clientFactory,
                                            IServiceProvider serviceProvider,
                                            ICharacterStore characterStore)
        {
            _logger = logger;
            _mapper = mapper;
            _clientFactory = clientFactory;
            _serviceProvider = serviceProvider;
            _characterStore = characterStore;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        using (_logger.BeginScope("Dehydrating characters"))
                        {
                            var correlationId = Guid.NewGuid();
                            var dehydrationService = scope.ServiceProvider.GetRequiredService<ICharacterDehydrationService>();
                            var requestClient = scope.ServiceProvider.CreateRequestClient<DehydrateCharacter>();
                            var options = new ParallelOptions
                            {
                                MaxDegreeOfParallelism = 8,
                                CancellationToken = cancellationToken
                            };
                            var watch = Stopwatch.StartNew();
                            await Parallel.ForEachAsync(_characterStore.FindAllAsync(), options, async (character, token) =>
                            {
                                var model = await dehydrationService.DehydrateAsync(character);
                                var request = _mapper.Map<DehydrateCharacter>(model) with { MasterId = character.MasterId, CorrelationId = correlationId };
                                var response = await requestClient.GetResponse<CharacterDehydrated>(request, token);
                            });
                            watch.Stop();
                            _logger.LogDebug("Dehydrating characters took {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error occurred dehydrating characters");
                }
            }
        }
    }
}