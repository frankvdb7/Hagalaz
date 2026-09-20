using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Services;
using Hagalaz.Services.Characters.Services.Model;
using MassTransit;
using State = Hagalaz.Services.Characters.Services.Model.State;

namespace Hagalaz.Services.Characters.Consumers
{
    public class GetCharacterRequestConsumer : IConsumer<GetCharacterRequest>
    {
        private readonly ICharacterService _characterService;
        private readonly ICharacterUnitOfWork _characterUnitOfWork;
        private readonly IMapper _mapper;

        public GetCharacterRequestConsumer(
            ICharacterService characterService,
            ICharacterUnitOfWork characterUnitOfWork,
            IMapper mapper)
        {
            _characterService = characterService;
            _characterUnitOfWork = characterUnitOfWork;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<GetCharacterRequest> context)
        {
            var message = context.Message;
            if (!(await _characterService.GetExistsAsync(message.MasterId, context.CancellationToken)).IsSuccess)
            {
                await context.RespondAsync(new CharacterNotFound(message.CorrelationId, message.MasterId));
                return;
            }
            var hydration = await _characterUnitOfWork.ExecuteConsistentReadAsync(async cancellationToken =>
            {
                var snapshotRevision = (await _characterService.GetSnapshotRevisionAsync(message.MasterId, cancellationToken)).Value;
                var result = await GetCharacterAsync(message.MasterId, cancellationToken);
                return (snapshotRevision, result);
            }, context.CancellationToken);
            var snapshotRevision = hydration.snapshotRevision;
            var result = hydration.result;
            var appearance = _mapper.Map<AppearanceDto>(result.appearance.ValueOrDefault);
            var statistics = _mapper.Map<StatisticsDto>(result.statistics.ValueOrDefault);
            var details = _mapper.Map<DetailsDto>(result.details.ValueOrDefault);
            var itemCollection = _mapper.Map<ItemCollectionDto>(result.items.ValueOrDefault);
            var familiar = _mapper.Map<FamiliarDto>(result.familiar.ValueOrDefault);
            var farming = _mapper.Map<FarmingDto>(result.farming.ValueOrDefault);
            var slayer = _mapper.Map<SlayerDto>(result.slayer.ValueOrDefault);
            var music = _mapper.Map<MusicDto>(result.music.ValueOrDefault);
            var notes = _mapper.Map<NotesDto>(result.notes.ValueOrDefault);
            var profile = _mapper.Map<ProfileDto>(result.profile.ValueOrDefault) ?? new ProfileDto();
            var itemAppearanceCollection = _mapper.Map<ItemAppearanceCollectionDto>(result.itemAppearances.ValueOrDefault);
            var state = _mapper.Map<StateDto>(result.state.ValueOrDefault);

            await context.RespondAsync(new GetCharacterResponse(message.CorrelationId, message.MasterId, appearance, details, statistics, itemCollection, familiar, music, farming, slayer, notes, profile, itemAppearanceCollection, state, snapshotRevision));
        }

        private async Task<(Result<Appearance> appearance,
            Result<Statistics> statistics,
            Result<Details> details,
            Result<IReadOnlyList<Item>> items,
            Result<Familiar> familiar,
            Result<Music> music,
            Result<Farming> farming,
            Result<Slayer> slayer,
            Result<Notes> notes,
            Result<ProfileModel> profile,
            Result<IReadOnlyList<ItemAppearance>> itemAppearances,
            Result<State> state)> GetCharacterAsync(uint masterId, CancellationToken cancellationToken)
        {
            var appearanceResult = await _characterService.GetAppearanceAsync(masterId, cancellationToken);
            var statisticsResult = await _characterService.GetStatisticsAsync(masterId, cancellationToken);
            var detailsResult = await _characterService.GetDetailsAsync(masterId, cancellationToken);
            var itemsResult = await _characterService.GetItemsAsync(masterId, cancellationToken);
            var familiarResult = await _characterService.GetFamiliarAsync(masterId, cancellationToken);
            var musicResult = await _characterService.GetMusicAsync(masterId, cancellationToken);
            var farmingResult = await _characterService.GetFarmingAsync(masterId, cancellationToken);
            var slayerResult = await _characterService.GetSlayerAsync(masterId, cancellationToken);
            var notesResult = await _characterService.GetNotesAsync(masterId, cancellationToken);
            var profileResult = await _characterService.GetProfileAsync(masterId, cancellationToken);
            var itemAppearanceResult = await _characterService.GetItemAppearancesAsync(masterId, cancellationToken);
            var stateResult = await _characterService.GetStateAsync(masterId, cancellationToken);

            return (appearanceResult, statisticsResult, detailsResult, itemsResult, familiarResult, musicResult, farmingResult, slayerResult, notesResult, profileResult, itemAppearanceResult, stateResult);
        }
    }
}
