using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hagalaz.Data.Entities;
using Hagalaz.Services.GameLogon.Data;
using Hagalaz.Services.GameLogon.Services.Model;
using Hagalaz.Services.GameLogon.Store;
using FriendsChatRank = Hagalaz.Services.GameLogon.Services.Model.FriendsChatRank;

namespace Hagalaz.Services.GameLogon.Services
{
    public class ContactService : IContactService
    {
        private readonly ICharacterUnitOfWork _unitOfWork;
        private readonly CharacterStore _characterStore;
        private readonly IMapper _mapper;
        private readonly ILogger<ContactService> _logger;

        public ContactService(ICharacterUnitOfWork unitOfWork, CharacterStore characterStore, IMapper mapper, ILogger<ContactService> logger)
        {
            _unitOfWork = unitOfWork;
            _characterStore = characterStore;
            _mapper = mapper;
            _logger = logger;
        }

        public async ValueTask<IReadOnlyList<ContactDto>> FindFriendsByIdAsync(uint masterId) =>
            await MapContactsAsync(_unitOfWork.CharacterContactsRepository.FindFriendsByMasterId(masterId), masterId).ToListAsync();

        public async ValueTask<ContactDto?> FindFriendByIdAsync(uint masterId, uint contactId) =>
            await _mapper.ProjectTo<ContactDto>(_unitOfWork.CharacterContactsRepository.FindFriendsByMasterId(masterId).Where(c => c.ContactId == contactId))
                .AsNoTracking()
                .SingleOrDefaultAsync();

        public async ValueTask<IReadOnlyList<ContactDto>> FindIgnoresByIdAsync(uint masterId) =>
            await _mapper.ProjectTo<ContactDto>(_unitOfWork.CharacterContactsRepository.FindIgnoresByMasterId(masterId)).ToListAsync();

        public async ValueTask<ContactDto?> FindIgnoreByIdAsync(uint masterId, uint contactId) =>
            await _mapper.ProjectTo<ContactDto>(_unitOfWork.CharacterContactsRepository.FindIgnoresByMasterId(masterId).Where(c => c.ContactId == contactId))
                .AsNoTracking()
                .SingleOrDefaultAsync();

        public async ValueTask<Result> AddContactAsync(uint masterId, uint contactId, bool ignore)
        {
            // can't add yourself to the contacts
            if (masterId == contactId)
            {
                return Result.Fail;
            }

            var result = Result.Success;
            try
            {
                await _unitOfWork.CharacterContactsRepository.AddContactAsync(new CharactersContact()
                {
                    MasterId = masterId, ContactId = contactId, Type = ignore ? (byte)1 : (byte)0, FcRank = (sbyte)FriendsChatRank.Friend
                });
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                result = Result.Fail;
                _logger.LogError(ex, "Failed to add contact");
                await _unitOfWork.RollbackAsync();
            }

            return result;
        }

        public async ValueTask<Result> RemoveContactAsync(uint masterId, uint contactId)
        {
            // can't remove yourself from the contacts
            if (masterId == contactId)
            {
                return Result.Fail;
            }

            var contact = await _unitOfWork.CharacterContactsRepository.FindContactByIdAsync(masterId, contactId).SingleOrDefaultAsync();
            if (contact == null)
            {
                return Result.NotFound;
            }

            var result = Result.Success;
            try
            {
                await _unitOfWork.CharacterContactsRepository.RemoveContactAsync(contact);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                result = Result.Fail;
                _logger.LogError(ex, "Failed to remove contact");
                await _unitOfWork.RollbackAsync();
            }

            return result;
        }

        public async ValueTask<Result> SetContactSettingsAsync(uint masterId, ContactSettingsDto settings)
        {
            var preferences = await _unitOfWork.CharacterPreferencesRepository.FindById(masterId).SingleOrDefaultAsync();
            if (preferences == null)
            {
                return Result.NotFound;
            }

            try
            {
                var availability = _mapper.Map<byte>(settings.Availability);
                preferences.FriendsFilter = availability;
                await _unitOfWork.CommitAsync();
                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set contact settings");
                await _unitOfWork.RollbackAsync();
                return Result.Fail;
            }
        }

        public async ValueTask<ContactSettingsDto?> FindContactSettingsAsync(uint masterId) =>
            await _mapper.ProjectTo<ContactSettingsDto>(_unitOfWork.CharacterPreferencesRepository.FindById(masterId)).AsNoTracking().SingleOrDefaultAsync();

        private async IAsyncEnumerable<ContactDto> MapContactsAsync(IQueryable<CharactersContact> contacts, uint masterId)
        {
            foreach (var contact in await _mapper.ProjectTo<ContactDto>(contacts).ToListAsync())
            {
                var friend = await FindFriendByIdAsync(contact.MasterId, masterId);
                var settings = await FindContactSettingsAsync(contact.MasterId);

                var contactSession = _characterStore.SingleOrDefault(context => context.Id == contact.MasterId);
                if (contactSession == null)
                {
                    // offline contact
                    yield return contact with
                    {
                        IsFriend = friend != null, Settings = settings
                    };
                    continue;
                }

                // online contact
                yield return contact with
                {
                    WorldId = contactSession.WorldId, IsFriend = friend != null, Settings = settings
                };
            }
        }
    }
}