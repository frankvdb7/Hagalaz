using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Exceptions;
using Hagalaz.FluentResults;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterUnitOfWork _characterUnitOfWork;
        private readonly IMapper _mapper;

        public CharacterService(ICharacterUnitOfWork characterUnitOfWork, IMapper mapper)
        {
            _characterUnitOfWork = characterUnitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<bool>> GetExistsAsync(uint masterId)
        {
            var exists = await _characterUnitOfWork.CharacterRepository.FindById(masterId).AsNoTracking().Select(c => c.Id).AnyAsync();
            if (!exists)
            {
                return ResultHelper.Fail(new NotFoundException("character"));
            }
            return exists;
        }

        public async Task<Result<Appearance>> GetAppearanceAsync(uint masterId)
        {
            var appearance = await _mapper.ProjectTo<Appearance>(_characterUnitOfWork.CharacterLookRepository.FindById(masterId).AsNoTracking()).FirstOrDefaultAsync();
            if (appearance == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(appearance)));
            }
            return appearance;
        }

        public async Task<Result<IReadOnlyList<ItemAppearance>>> GetItemAppearancesAsync(uint masterId)
        {
            var items = await _mapper.ProjectTo<ItemAppearance>(_characterUnitOfWork.CharacterItemLookRepository.FindById(masterId)).ToListAsync();
            if (items == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(items)));
            }
            return items;
        }

        public async Task<Result<Details>> GetDetailsAsync(uint masterId)
        {
            var details = await _mapper.ProjectTo<Details>(_characterUnitOfWork.CharacterRepository.FindById(masterId).AsNoTracking()).FirstOrDefaultAsync();
            if (details == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(details)));
            }
            return details;
        }

        public async Task<Result<Familiar>> GetFamiliarAsync(uint masterId)
        {
            var familiar = await _mapper.ProjectTo<Familiar>(_characterUnitOfWork.CharacterFamiliarRepository.FindById(masterId).AsNoTracking()).FirstOrDefaultAsync();
            if (familiar == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(familiar)));
            }
            return familiar;
        }

        public async Task<Result<Farming>> GetFarmingAsync(uint masterId)
        {
            var patches = await _mapper.ProjectTo<Farming.Patch>(_characterUnitOfWork.CharacterFarmingRepository.FindById(masterId).AsNoTracking()).ToListAsync();
            if (patches == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(patches)));
            }
            return new Farming
            {
                Patches = patches
            };
        }

        public async Task<Result<IReadOnlyList<Item>>> GetItemsAsync(uint masterId)
        {
            var items = await _mapper.ProjectTo<Item>(_characterUnitOfWork.CharacterItemRepository.FindByMasterId(masterId).AsNoTracking()).ToListAsync();
            return items;
        }

        public async Task<Result<Music>> GetMusicAsync(uint masterId)
        {
            var music = await _characterUnitOfWork.CharacterMusicRepository.FindById(masterId).AsNoTracking().FirstOrDefaultAsync();
            if (music == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(music)));
            }
            var playlist = await _characterUnitOfWork.CharacterMusicPlaylistRepository.FindById(masterId).AsNoTracking().FirstOrDefaultAsync();
            if (playlist == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(playlist)));
            }
            var musicAndPlaylist = new Music();
            musicAndPlaylist = _mapper.Map(music, musicAndPlaylist);
            musicAndPlaylist = _mapper.Map(playlist, musicAndPlaylist);
            return musicAndPlaylist;
        }

        public async Task<Result<Notes>> GetNotesAsync(uint masterId)
        {
            var notes = await _mapper.ProjectTo<Notes.Note>(_characterUnitOfWork.CharacterNotesRepository.FindById(masterId).AsNoTracking()).ToListAsync();
            if (notes == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(notes)));
            }
            return new Notes
            {
                AllNotes = notes
            };
        }

        public async Task<Result<TValue>> GetProfileDataByKeyAsync<TValue>(uint masterId, string key)
        {
            var data = await _characterUnitOfWork.CharacterProfileRepository.FindProfileDataByKey(masterId, key).AsNoTracking().FirstOrDefaultAsync();
            if (data == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(data)));
            }
            try
            {
                var tData = JsonSerializer.Deserialize<TValue>(data);
                if (tData == null)
                {
                    return ResultHelper.Fail(new NotFoundException(nameof(tData)));
                }
                return tData;
            } 
            catch (Exception ex)
            {
                return ResultHelper.Fail(ex);
            }
        }

        public async Task<Result<ProfileModel>> GetProfileAsync(uint masterId)
        {
            var profile = await _mapper.ProjectTo<ProfileModel>(_characterUnitOfWork.CharacterProfileRepository.FindProfileById(masterId).AsNoTracking()).FirstOrDefaultAsync();
            if (profile == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(profile)));
            }
            return profile;
        }

        public async Task<Result<Slayer>> GetSlayerAsync(uint masterId)
        {
            var task = await _mapper.ProjectTo<Slayer.SlayerTask>(_characterUnitOfWork.CharacterSlayerRepository.FindById(masterId).AsNoTracking()).FirstOrDefaultAsync();
            if (task == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(task)));
            }
            return new Slayer
            {
                Task = task
            };
        }

        public async Task<Result<Statistics>> GetStatisticsAsync(uint masterId)
        {
            var statistics = await _mapper.ProjectTo<Statistics>(_characterUnitOfWork.CharacterStatisticsRepository.FindById(masterId).AsNoTracking()).FirstOrDefaultAsync();
            if (statistics == null)
            {
                return ResultHelper.Fail(new NotFoundException(nameof(statistics)));
            }
            return statistics;
        }

        public async Task<Result<State>> GetStateAsync(uint masterId)
        {
            var states = await _mapper.ProjectTo<State.StateEx>(_characterUnitOfWork.CharacterStateRepository.FindAll().Where(c => c.MasterId == masterId).AsNoTracking()).ToListAsync();
            return new State { StatesEx = states };
        }
    }
}
