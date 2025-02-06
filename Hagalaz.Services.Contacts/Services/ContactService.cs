using System.Text.Json;
using AutoMapper;
using Hagalaz.Configuration;
using Hagalaz.Contacts.Messages.Model;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Model;
using Hagalaz.Services.Contacts.Data;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Text.Json;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using ContactDto = Hagalaz.Services.Contacts.Services.Model.ContactDto;
using ContactSettingsDto = Hagalaz.Services.Contacts.Services.Model.ContactSettingsDto;

namespace Hagalaz.Services.Contacts.Services
{
    using ContactDto = Model.ContactDto;
    using ContactSettingsDto = Model.ContactSettingsDto;

    public class ContactService : IContactService
    {
        private readonly ICharacterUnitOfWork _unitOfWork;
        private readonly ContactSessionStore _contactStore;
        private readonly IMapper _mapper;
        private readonly ILogger<ContactService> _logger;

        public ContactService(ICharacterUnitOfWork unitOfWork, ContactSessionStore contactStore, IMapper mapper, ILogger<ContactService> logger)
        {
            _unitOfWork = unitOfWork;
            _contactStore = contactStore;
            _mapper = mapper;
            _logger = logger;
        }

        public async ValueTask<IReadOnlyList<ContactDto>> FindFriendsByIdAsync(uint masterId) =>
            await MapContactsAsync(_unitOfWork.CharacterContactsRepository.FindFriendsByMasterId(masterId), masterId).ToListAsync();

        public async ValueTask<ContactDto?> FindFriendByIdAsync(uint masterId, uint contactId) =>
            await _mapper.ProjectTo<ContactDto>(_unitOfWork.CharacterContactsRepository.FindFriendsByMasterId(masterId).Where(c => c.ContactId == contactId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

        public async ValueTask<IReadOnlyList<ContactDto>> FindIgnoresByIdAsync(uint masterId) =>
            await _mapper.ProjectTo<ContactDto>(_unitOfWork.CharacterContactsRepository.FindIgnoresByMasterId(masterId)).ToListAsync();

        public async ValueTask<ContactDto?> FindIgnoreByIdAsync(uint masterId, uint contactId) =>
            await _mapper.ProjectTo<ContactDto>(_unitOfWork.CharacterContactsRepository.FindIgnoresByMasterId(masterId).Where(c => c.ContactId == contactId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

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
                _unitOfWork.CharacterContactsRepository.AddContact(new CharactersContact()
                {
                    MasterId = masterId,
                    ContactId = contactId,
                    Type = ignore ? (byte)1 : (byte)0,
                    FcRank = (sbyte)FriendsChatRank.Friend
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

            var contact = await _unitOfWork.CharacterContactsRepository.FindContactByIdAsync(masterId, contactId).FirstOrDefaultAsync();
            if (contact == null)
            {
                return Result.NotFound;
            }

            var result = Result.Success;
            try
            {
                _unitOfWork.CharacterContactsRepository.RemoveContact(contact);
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
            var profile = await _unitOfWork.CharacterProfilesRepository.FindById(masterId).FirstOrDefaultAsync();
            if (profile == null)
            {
                return Result.NotFound;
            }

            try
            {
                var availability = _mapper.Map<byte>(settings.Availability);
                var dictionary = new JsonDictionary(ProfileConstants.DefaultOptions);
                dictionary.FromJson(profile.Data);
                dictionary.SetValue(ProfileConstants.ChatSettingsFriendsFilter, availability);
                profile.Data = dictionary.ToJson();
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
            await _mapper.ProjectTo<ContactSettingsDto>(_unitOfWork.CharacterProfilesRepository.FindById(masterId)).AsNoTracking().FirstOrDefaultAsync();

        private async IAsyncEnumerable<ContactDto> MapContactsAsync(IQueryable<CharactersContact> contacts, uint masterId)
        {
            foreach (var contact in await _mapper.ProjectTo<ContactDto>(contacts).ToListAsync())
            {
                var mutualFriend = await FindFriendByIdAsync(contact.MasterId, masterId);
                var settings = await FindContactSettingsAsync(contact.MasterId);

                if (settings != null)
                {
                    if (settings.Availability.Off)
                    {
                        // offline contact
                        yield return contact with
                        {
                            AreMutualFriends = mutualFriend != null, Settings = settings
                        };
                        continue;
                    }
                    else if (settings.Availability.Friends && mutualFriend == null)
                    {
                        // offline contact
                        yield return contact with
                        {
                            AreMutualFriends = false, Settings = settings
                        };
                        continue;
                    }
                }

                var contactSession = _contactStore.GetOrDefault(contact.MasterId);
                if (contactSession == null)
                {
                    // offline contact
                    yield return contact with
                    {
                        AreMutualFriends = mutualFriend != null, Settings = settings
                    };
                    continue;
                }

                // online contact
                yield return contact with
                {
                    WorldId = contactSession.WorldId, WorldName = contactSession.WorldName, AreMutualFriends = mutualFriend != null, Settings = settings
                };
            }
        }
    }
}