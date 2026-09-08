using System;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.StateMachines;
using Hagalaz.Services.GameWorld.Logic.Characters.States;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Extensions;

public static class CharacterHydrationRegistrationExtensions
{
    public static void AddWorldCharacterHydration(this IBusRegistrationConfigurator registration, WorldInstanceIdentity identity)
    {
        // Replies must reach the process whose in-memory repository owns the saga.
        var endpointName = $"hagalaz-gameworld-hydration-{identity.InstanceId}";
        registration.AddSagaStateMachine<CharacterHydrationStateMachine, CharacterHydrationState>()
            .InMemoryRepository()
            .Endpoint(endpoint =>
            {
                endpoint.Name = endpointName;
                endpoint.Temporary = true;
            });
        registration.AddRequestClient<HydrateCharacter>(new Uri($"exchange:{endpointName}?durable=false&autodelete=true"));
    }
}
