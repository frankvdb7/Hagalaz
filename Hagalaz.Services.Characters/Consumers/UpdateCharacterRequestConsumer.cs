using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Services.Characters.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.Characters.Consumers
{
    public class UpdateCharacterRequestConsumer : IConsumer<UpdateCharacterRequest>, IConsumer<PersistCharacterCommand>
    {
        private readonly ICharacterUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateCharacterRequestConsumer(ICharacterUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<UpdateCharacterRequest> context)
        {
            var message = context.Message;
            Validate(message);

            var character = await _unitOfWork.CharacterRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            if (character == null)
            {
                await context.RespondAsync(new CharacterNotFound(message.CorrelationId, message.MasterId));
                return;
            }

            if (message.SnapshotRevision <= character.SnapshotRevision)
            {
                await context.RespondAsync(new UpdateCharacterResponse(message.CorrelationId, message.MasterId));
                return;
            }

            await ApplySnapshotAsync(message, character);
            await context.RespondAsync(new UpdateCharacterResponse(message.CorrelationId, message.MasterId));
        }

        public async Task Consume(ConsumeContext<PersistCharacterCommand> context)
        {
            var message = context.Message;
            Validate(message);

            var character = await _unitOfWork.CharacterRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            if (character == null)
            {
                throw new InvalidOperationException($"Character '{message.MasterId}' was not found while applying a persistence command.");
            }

            if (message.SnapshotRevision <= character.SnapshotRevision)
            {
                return;
            }

            await ApplySnapshotAsync(message, character);
        }

        private async Task ApplySnapshotAsync(ICharacterPersistenceMessage message, Hagalaz.Data.Entities.Character character)
        {
            character.SnapshotRevision = message.SnapshotRevision;

            character.CoordX = checked((short)message.Details.CoordX);
            character.CoordY = checked((short)message.Details.CoordY);
            character.CoordZ = checked((byte)message.Details.CoordZ);

            var appearance = await _unitOfWork.CharacterLookRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            if (appearance == null)
            {
                appearance = new CharactersLook { MasterId = message.MasterId };
                _unitOfWork.Add(appearance);
            }
            _mapper.Map(message.Appearance, appearance);

            var statistics = await _unitOfWork.CharacterStatisticsRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            if (statistics == null)
            {
                statistics = new CharactersStatistic { MasterId = message.MasterId };
                _unitOfWork.Add(statistics);
            }
            _mapper.Map(message.Statistics, statistics);

            await ReplaceItemsAsync(message);
            await ReplaceFarmingAsync(message);
            await ReplaceNotesAsync(message);
            await ReplaceItemAppearancesAsync(message);
            await ReplaceStatesAsync(message);

            await UpdateFamiliarAsync(message);
            await UpdateMusicAsync(message);
            await UpdateProfileAsync(message);
            await UpdateSlayerAsync(message);

            await _unitOfWork.CommitAsync();
        }

        private async Task UpdateFamiliarAsync(ICharacterPersistenceMessage message)
        {
            var familiar = await _unitOfWork.CharacterFamiliarRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            // Game-world dehydration returns the default DTO (FamiliarId = 0) when no familiar script is active.
            if (message.Familiar == null || message.Familiar.FamiliarId <= 0)
            {
                if (familiar != null)
                {
                    _unitOfWork.Remove(familiar);
                }
                return;
            }

            if (familiar == null)
            {
                familiar = new CharactersFamiliar { MasterId = message.MasterId };
                _unitOfWork.Add(familiar);
            }
            _mapper.Map(message.Familiar, familiar);
        }

        private async Task UpdateMusicAsync(ICharacterPersistenceMessage message)
        {
            var music = await _unitOfWork.CharacterMusicRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            if (music == null)
            {
                music = new CharactersMusic { MasterId = message.MasterId };
                _unitOfWork.Add(music);
            }
            _mapper.Map(message.Music, music);

            var playlist = await _unitOfWork.CharacterMusicPlaylistRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            if (playlist == null)
            {
                playlist = new CharactersMusicPlaylist { MasterId = message.MasterId };
                _unitOfWork.Add(playlist);
            }
            _mapper.Map(message.Music, playlist);
        }

        private async Task UpdateProfileAsync(ICharacterPersistenceMessage message)
        {
            var profile = await _unitOfWork.CharacterProfileRepository.FindProfileById(message.MasterId).SingleOrDefaultAsync();
            if (profile == null)
            {
                profile = new CharactersProfile { MasterId = message.MasterId };
                _unitOfWork.Add(profile);
            }
            _mapper.Map(message.Profile, profile);
        }

        private async Task UpdateSlayerAsync(ICharacterPersistenceMessage message)
        {
            var slayer = await _unitOfWork.CharacterSlayerRepository.FindById(message.MasterId).SingleOrDefaultAsync();
            // Game-world dehydration represents an inactive Slayer task as a task with Id = -1.
            if (message.Slayer.Task == null || message.Slayer.Task.Id < 0)
            {
                if (slayer != null)
                {
                    _unitOfWork.Remove(slayer);
                }
                return;
            }

            if (slayer == null)
            {
                slayer = new CharactersSlayerTask { MasterId = message.MasterId };
                _unitOfWork.Add(slayer);
            }
            _mapper.Map(message.Slayer.Task, slayer);
        }

        private async Task ReplaceItemsAsync(ICharacterPersistenceMessage message)
        {
            var existing = await _unitOfWork.CharacterItemRepository.FindByMasterId(message.MasterId).ToListAsync();
            foreach (var item in existing)
            {
                _unitOfWork.Remove(item);
            }

            AddItems(message.MasterId, message.ItemCollection.Bank, ItemContainerType.Bank);
            AddItems(message.MasterId, message.ItemCollection.Inventory, ItemContainerType.Inventory);
            AddItems(message.MasterId, message.ItemCollection.FamiliarInventory, ItemContainerType.FamiliarInventory);
            AddItems(message.MasterId, message.ItemCollection.Equipment, ItemContainerType.Equipment);
            AddItems(message.MasterId, message.ItemCollection.Rewards, ItemContainerType.Reward);
            AddItems(message.MasterId, message.ItemCollection.MoneyPouch, ItemContainerType.MoneyPouch);
        }

        private void AddItems(uint masterId, IEnumerable<ItemDto> items, ItemContainerType containerType)
        {
            foreach (var itemDto in items)
            {
                var item = _mapper.Map<CharactersItem>(itemDto);
                item.MasterId = masterId;
                item.ContainerType = (sbyte)containerType;
                _unitOfWork.Add(item);
            }
        }

        private async Task ReplaceFarmingAsync(ICharacterPersistenceMessage message)
        {
            var existing = await _unitOfWork.CharacterFarmingRepository.FindById(message.MasterId).ToListAsync();
            var incoming = message.Farming.Patches.ToDictionary(patch => checked((uint)patch.Id));
            foreach (var patch in existing)
            {
                if (!incoming.TryGetValue(patch.PatchId, out var patchDto))
                {
                    _unitOfWork.Remove(patch);
                    continue;
                }

                _mapper.Map(patchDto, patch);
                incoming.Remove(patch.PatchId);
            }

            foreach (var patchDto in incoming.Values)
            {
                var patch = _mapper.Map<CharactersFarmingPatch>(patchDto);
                patch.MasterId = message.MasterId;
                _unitOfWork.Add(patch);
            }
        }

        private async Task ReplaceNotesAsync(ICharacterPersistenceMessage message)
        {
            var existing = await _unitOfWork.CharacterNotesRepository.FindById(message.MasterId).ToListAsync();
            var incoming = message.Notes.Notes.ToDictionary(note => checked((byte)note.Id));
            foreach (var note in existing)
            {
                if (!incoming.TryGetValue(note.NoteId, out var noteDto))
                {
                    _unitOfWork.Remove(note);
                    continue;
                }

                _mapper.Map(noteDto, note);
                incoming.Remove(note.NoteId);
            }

            foreach (var noteDto in incoming.Values)
            {
                var note = _mapper.Map<CharactersNote>(noteDto);
                note.MasterId = message.MasterId;
                _unitOfWork.Add(note);
            }
        }

        private async Task ReplaceItemAppearancesAsync(ICharacterPersistenceMessage message)
        {
            var existing = await _unitOfWork.CharacterItemLookRepository.FindById(message.MasterId).ToListAsync();
            var incoming = message.ItemAppearanceCollection.Appearances.ToDictionary(appearance => checked((ushort)appearance.Id));
            foreach (var appearance in existing)
            {
                if (!incoming.TryGetValue(appearance.ItemId, out var appearanceDto))
                {
                    _unitOfWork.Remove(appearance);
                    continue;
                }

                _mapper.Map(appearanceDto, appearance);
                incoming.Remove(appearance.ItemId);
            }

            foreach (var appearanceDto in incoming.Values)
            {
                var appearance = _mapper.Map<CharactersItemsLook>(appearanceDto);
                appearance.MasterId = message.MasterId;
                _unitOfWork.Add(appearance);
            }
        }

        private async Task ReplaceStatesAsync(ICharacterPersistenceMessage message)
        {
            var existing = await _unitOfWork.CharacterStateRepository.FindAll().Where(s => s.MasterId == message.MasterId).ToListAsync();
            var incoming = message.State.StatesEx.ToDictionary(state => state.Id.ToString(CultureInfo.InvariantCulture));
            foreach (var state in existing)
            {
                if (!incoming.TryGetValue(state.StateId, out var stateDto))
                {
                    _unitOfWork.Remove(state);
                    continue;
                }

                _mapper.Map(stateDto, state);
                incoming.Remove(state.StateId);
            }

            foreach (var stateDto in incoming.Values)
            {
                var state = _mapper.Map<CharactersState>(stateDto);
                state.MasterId = message.MasterId;
                _unitOfWork.Add(state);
            }
        }

        private static void Validate(ICharacterPersistenceMessage message)
        {
            ArgumentNullException.ThrowIfNull(message.Appearance);
            ArgumentNullException.ThrowIfNull(message.Details);
            ArgumentNullException.ThrowIfNull(message.Statistics);
            ArgumentNullException.ThrowIfNull(message.ItemCollection);
            ArgumentNullException.ThrowIfNull(message.Music);
            ArgumentNullException.ThrowIfNull(message.Farming);
            ArgumentNullException.ThrowIfNull(message.Slayer);
            ArgumentNullException.ThrowIfNull(message.Notes);
            ArgumentNullException.ThrowIfNull(message.Profile);
            ArgumentNullException.ThrowIfNull(message.ItemAppearanceCollection);
            ArgumentNullException.ThrowIfNull(message.State);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(message.SnapshotRevision);

            ArgumentNullException.ThrowIfNull(message.ItemCollection.Bank);
            ArgumentNullException.ThrowIfNull(message.ItemCollection.Inventory);
            ArgumentNullException.ThrowIfNull(message.ItemCollection.FamiliarInventory);
            ArgumentNullException.ThrowIfNull(message.ItemCollection.Equipment);
            ArgumentNullException.ThrowIfNull(message.ItemCollection.Rewards);
            ArgumentNullException.ThrowIfNull(message.ItemCollection.MoneyPouch);
            ArgumentNullException.ThrowIfNull(message.Farming.Patches);
            ArgumentNullException.ThrowIfNull(message.Notes.Notes);
            ArgumentNullException.ThrowIfNull(message.ItemAppearanceCollection.Appearances);
            ArgumentNullException.ThrowIfNull(message.State.StatesEx);
        }

    }
}
