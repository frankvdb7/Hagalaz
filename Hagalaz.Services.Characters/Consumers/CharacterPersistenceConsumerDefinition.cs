using System;
using Hagalaz.Data;
using MassTransit;

namespace Hagalaz.Services.Characters.Consumers;

/// <summary>
/// Keeps transient character-service/database failures inside the durable command
/// delivery path instead of immediately faulting the persistence message.
/// </summary>
public sealed class CharacterPersistenceConsumerDefinition : ConsumerDefinition<UpdateCharacterRequestConsumer>
{
    public CharacterPersistenceConsumerDefinition()
    {
        EndpointName = "UpdateCharacterRequest";
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UpdateCharacterRequestConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Keep failed commands durably available in RabbitMQ's endpoint error
        // queue after retries. Skipped/unmatched messages use the dead-letter
        // queue instead of being silently discarded.
        endpointConfigurator.ConfigureDefaultErrorTransport();
        endpointConfigurator.ConfigureDefaultDeadLetterTransport();
        endpointConfigurator.PublishFaults = true;
        endpointConfigurator.UseEntityFrameworkOutbox<HagalazDbContext>(context);
        endpointConfigurator.UseMessageRetry(retry =>
            retry.Exponential(
                retryLimit: 5,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(5)));
    }
}
