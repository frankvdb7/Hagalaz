using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Services.Characters.Services;
using Hagalaz.Services.Characters.Services.Model;
using MassTransit;

namespace Hagalaz.Services.Characters.Consumers
{
    public class GetCharacterRequestConsumer : IConsumer<GetCharacterRequest>
    {
        private readonly ICharacterService _characterService;
        private readonly IMapper _mapper;

        public GetCharacterRequestConsumer(ICharacterService characterService, IMapper mapper)
        {
            _characterService = characterService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<GetCharacterRequest> context)
        {
            var message = context.Message;
            if (!(await _characterService.GetExistsAsync(message.MasterId)).IsSuccess)
            {
                await context.RespondAsync(new CharacterNotFound(message.CorrelationId, message.MasterId));
                return;
            }
            var result = await GetCharacterAsync(message.MasterId);
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

            await context.RespondAsync(new GetCharacterResponse(message.CorrelationId, message.MasterId, appearance, details, statistics, itemCollection, familiar, music, farming, slayer, notes, profile, itemAppearanceCollection, state));
        }

        public async Task<(Result<Appearance> appearance,
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
            Result<Services.Model.State> state)> GetCharacterAsync(uint masterId)
        {
            var appearanceResult = await _characterService.GetAppearanceAsync(masterId);
            var statisticsResult = await _characterService.GetStatisticsAsync(masterId);
            var detailsResult = await _characterService.GetDetailsAsync(masterId);
            var itemsResult = await _characterService.GetItemsAsync(masterId);
            var familiarResult = await _characterService.GetFamiliarAsync(masterId);
            var musicResult = await _characterService.GetMusicAsync(masterId);
            var farmingResult = await _characterService.GetFarmingAsync(masterId);
            var slayerResult = await _characterService.GetSlayerAsync(masterId);
            var notesResult = await _characterService.GetNotesAsync(masterId);
            var profileResult = await _characterService.GetProfileAsync(masterId);
            var itemAppearanceResult = await _characterService.GetItemAppearancesAsync(masterId);
            var stateResult = await _characterService.GetStateAsync(masterId);

            return (appearanceResult, statisticsResult, detailsResult, itemsResult, familiarResult, musicResult, farmingResult, slayerResult, notesResult, profileResult, itemAppearanceResult, stateResult);
        }
    }
}
