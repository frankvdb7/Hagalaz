using System;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.States;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.StateMachines
{
    public class CharacterDehydrationStateMachine : MassTransitStateMachine<CharacterDehydrationState>
    {
        public CharacterDehydrationStateMachine(IMapper mapper)
        {
            InstanceState(x => x.CurrentState);

            Event(() => DehydrateCharacter, x => x.CorrelateById(context => context.Message.CorrelationId));
            Request(() => RequestUpdateCharacter, state => state.UpdateCharacterRequestId, x => x.Timeout = TimeSpan.FromSeconds(30));

            Initially(
                When(DehydrateCharacter)
                    .Then(context =>
                    {
                        context.Saga.MasterId = context.Message.MasterId;
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.UpdateCharacterRequestId = Guid.NewGuid();
                        context.Saga.RequestId = context.RequestId;
                        context.Saga.ResponseAddress = context.ResponseAddress;
                        context.Saga.Details = context.Message.Details;
                        context.Saga.Appearance = context.Message.Appearance;
                        context.Saga.Statistics = context.Message.Statistics;
                        context.Saga.ItemCollection = context.Message.ItemCollection;
                        context.Saga.Familiar = context.Message.Familiar;
                        context.Saga.Notes = context.Message.Notes;
                        context.Saga.Profile = context.Message.Profile;
                        context.Saga.ItemAppearanceCollection = context.Message.ItemAppearanceCollection;
                        context.Saga.Music = context.Message.Music;
                        context.Saga.Farming = context.Message.Farming;
                        context.Saga.Slayer = context.Message.Slayer;
                        context.Saga.State = context.Message.State;
                    })
                    .Request(RequestUpdateCharacter, context =>
                    {
                        var appearance = mapper.Map<AppearanceDto>(context.Saga.Appearance);
                        var details = mapper.Map<DetailsDto>(context.Saga.Details);
                        var statistics = mapper.Map<StatisticsDto>(context.Saga.Statistics);
                        var items = mapper.Map<ItemCollectionDto>(context.Saga.ItemCollection);
                        var familiar = mapper.Map<FamiliarDto>(context.Saga.Familiar);
                        var notes = mapper.Map<NotesDto>(context.Saga.Notes);
                        var profile = mapper.Map<ProfileDto>(context.Saga.Profile);
                        var itemAppearanceCollection = mapper.Map<ItemAppearanceCollectionDto>(context.Saga.ItemAppearanceCollection);
                        var farming = mapper.Map<FarmingDto>(context.Saga.Farming);
                        var slayer = mapper.Map<SlayerDto>(context.Saga.Slayer);
                        var music = mapper.Map<MusicDto>(context.Saga.Music);
                        var state = mapper.Map<StateDto>(context.Saga.State);
                        return new UpdateCharacterRequest(context.Saga.CorrelationId, context.Saga.MasterId, appearance, details, statistics, items, familiar, music, farming, slayer, notes, profile, itemAppearanceCollection, state);
                    })
                    .TransitionTo(Requested));

            During(Requested,
                When(RequestUpdateCharacter.Completed)
                    .ThenAsync(async context =>
                    {
                        var endpoint = await context.GetSendEndpoint(context.Saga.ResponseAddress!);
                        await endpoint.Send(
                            new CharacterDehydrated
                            {
                                MasterId = context.Saga.MasterId,
                                CorrelationId = context.Saga.CorrelationId
                            },
                            r => r.RequestId = context.Saga.RequestId);
                    })
                    .Finalize(),
                When(RequestUpdateCharacter.Completed2)
                    .ThenAsync(async context =>
                    {
                        var endpoint = await context.GetSendEndpoint(context.Saga.ResponseAddress!);
                        await endpoint.Send(
                            context.Message,
                            r => r.RequestId = context.Saga.RequestId);
                    }),
                When(RequestUpdateCharacter.Faulted)
                    .TransitionTo(Failed),
                When(RequestUpdateCharacter.TimeoutExpired)
                    .TransitionTo(Failed));

            SetCompletedWhenFinalized();
        }

        public Event<DehydrateCharacter> DehydrateCharacter { get; private set; } = default!;
        public Request<CharacterDehydrationState, UpdateCharacterRequest, UpdateCharacterResponse, CharacterNotFound> RequestUpdateCharacter { get; private set; } = default!;

        public State Requested { get; private set; } = default!;
        public State Failed { get; private set; } = default!;
    }
}
