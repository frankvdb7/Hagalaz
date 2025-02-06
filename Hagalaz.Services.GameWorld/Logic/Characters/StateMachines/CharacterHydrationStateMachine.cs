using System;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Logic.Characters.States;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.StateMachines
{
    public class CharacterHydrationStateMachine : MassTransitStateMachine<CharacterHydrationState>
    {
        public CharacterHydrationStateMachine(IMapper mapper)
        {
            InstanceState(x => x.CurrentState);

            Event(() => HydrateCharacter, x => x.CorrelateById(context => context.Message.CorrelationId));
            Request(() => RequestCharacter, state => state.GetCharacterRequestId, x => x.Timeout = TimeSpan.FromSeconds(10));

            Initially(
                When(HydrateCharacter)
                    .Then(context =>
                    {
                        context.Saga.MasterId = context.Message.MasterId;
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.GetCharacterRequestId = Guid.NewGuid();
                        context.Saga.RequestId = context.RequestId;
                        context.Saga.ResponseAddress = context.ResponseAddress;
                    })
                    .Request(RequestCharacter, context => new GetCharacterRequest(context.Saga.CorrelationId, context.Saga.MasterId))
                    .TransitionTo(Requested));

            During(Requested,
                When(RequestCharacter.Completed)
                    .ThenAsync(async context =>
                    {
                        var message = context.Message;
                        context.Saga.Appearance = mapper.Map<HydratedAppearanceDto>(message.Appearance);
                        context.Saga.Details = mapper.Map<HydratedDetailsDto>(message.Details);
                        context.Saga.Statistics = mapper.Map<HydratedStatisticsDto>(message.Statistics);
                        context.Saga.ItemCollection = mapper.Map<HydratedItemCollectionDto>(message.ItemCollection);
                        context.Saga.Familiar = mapper.Map<HydratedFamiliarDto>(message.Familiar);
                        context.Saga.Music = mapper.Map<HydratedMusicDto>(message.Music);
                        context.Saga.Farming = mapper.Map<HydratedFarmingDto>(message.Farming);
                        context.Saga.Slayer = mapper.Map<HydratedSlayerDto>(message.Slayer);
                        context.Saga.Notes = mapper.Map<HydratedNotesDto>(message.Notes);
                        context.Saga.Profile = mapper.Map<HydratedProfileDto>(message.Profile);
                        context.Saga.ItemAppearanceCollection = mapper.Map<HydratedItemAppearanceCollectionDto>(message.ItemAppearanceCollection);
                        context.Saga.State = mapper.Map<HydratedStateDto>(message.State);

                        var endpoint = await context.GetSendEndpoint(context.Saga.ResponseAddress!);
                        await endpoint.Send(
                            new CharacterHydrated
                            {
                                MasterId = context.Saga.MasterId,
                                CorrelationId = context.Saga.CorrelationId,
                                Appearance = context.Saga.Appearance,
                                Details = context.Saga.Details,
                                ItemCollection = context.Saga.ItemCollection,
                                Statistics = context.Saga.Statistics,
                                Familiar = context.Saga.Familiar,
                                Music = context.Saga.Music,
                                Farming = context.Saga.Farming,
                                Slayer = context.Saga.Slayer,
                                Notes = context.Saga.Notes,
                                Profile = context.Saga.Profile,
                                ItemAppearanceCollection = context.Saga.ItemAppearanceCollection,
                                State = context.Saga.State
                            },
                            r => r.RequestId = context.Saga.RequestId);
                    })
                    .Finalize(),
                When(RequestCharacter.Completed2)
                    .ThenAsync(async context =>
                    {
                        var endpoint = await context.GetSendEndpoint(context.Saga.ResponseAddress!);
                        await endpoint.Send(
                            context.Message,
                            r => r.RequestId = context.Saga.RequestId);
                    }),
                When(RequestCharacter.Faulted)
                    .TransitionTo(Failed),
                When(RequestCharacter.TimeoutExpired)
                    .TransitionTo(Failed));

            SetCompletedWhenFinalized();
        }

        public Event<HydrateCharacter> HydrateCharacter { get; private set; } = default!;
        public Request<CharacterHydrationState, GetCharacterRequest, GetCharacterResponse, CharacterNotFound> RequestCharacter { get; private set; } = default!;

        public State Requested { get; private set; } = default!;
        public State Failed { get; private set; } = default!;
    }
}
