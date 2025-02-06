using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hagalaz.Data.Entities;
using Hagalaz.Services.GameLogon.Config;
using Hagalaz.Services.GameLogon.Data;
using Hagalaz.Services.GameLogon.Services.Model;
using Hagalaz.Services.GameLogon.Store;
using Hagalaz.Services.GameLogon.Store.Model;
using FriendsChatRank = Hagalaz.Services.GameLogon.Services.Model.FriendsChatRank;

namespace Hagalaz.Services.GameLogon.Services
{
    using FriendsChatRank = Model.FriendsChatRank;

    public class FriendsChatService : IFriendsChatService
    {
        private readonly ICharacterUnitOfWork _unitOfWork;
        private readonly FriendsChatStore _chatStore;
        private readonly CharacterStore _characterStore;
        private readonly IMapper _mapper;
        private readonly FriendsChatOptions _options;

        public FriendsChatService(
            ICharacterUnitOfWork unitOfWork,
            FriendsChatStore chatStore,
            CharacterStore characterStore,
            IMapper mapper,
            IOptions<FriendsChatOptions> options)
        {
            _unitOfWork = unitOfWork;
            _chatStore = chatStore;
            _characterStore = characterStore;
            _mapper = mapper;
            _options = options.Value;
        }

        public async ValueTask<FriendsChatRegisterResult> RegisterMemberAsync(string ownerDisplayName, uint characterId)
        {
            var session = _characterStore[characterId];
            if (session == null)
            {
                return FriendsChatRegisterResult.Fail;
            }

            var owner = await _unitOfWork.characterStore.FindByDisplayNameAsync(ownerDisplayName)
                .Select(x => new
                {
                    x.Id
                })
                .AsNoTracking()
                .SingleOrDefaultAsync();
            if (owner == null)
            {
                // the character is not found
                return FriendsChatRegisterResult.NotFound;
            }

            var ownerPreferences = await _unitOfWork.CharacterPreferencesRepository.FindById(owner.Id)
                .Select(x => new
                {
                    x.FcName, x.FcRankEnter
                })
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (string.IsNullOrEmpty(ownerPreferences?.FcName))
            {
                // the character found does not have a Friends Chat Channel
                return FriendsChatRegisterResult.NotFound;
            }

            // check whether the channel owner tries to join the channel
            if (owner.Id != session.Id)
            {
                var contact = await _unitOfWork.CharacterContactsRepository.FindContactByIdAsync(owner.Id, session.Id)
                    .Select(c => new
                    {
                        c.FcRank
                    })
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
                var contactRank = contact == null ? FriendsChatRank.Regular : (FriendsChatRank)contact.FcRank;
                if ((FriendsChatRank)ownerPreferences.FcRankEnter > contactRank)
                {
                    // too low rank
                    return FriendsChatRegisterResult.Unauthorized;
                }
            }

            var chat = _chatStore[ownerPreferences.FcName] ??= new FriendsChatContext(ownerPreferences.FcName, owner.Id);
            if (chat.Members.Contains(characterId))
            {
                // something went wrong
                return FriendsChatRegisterResult.Fail;
            }

            // reserve a single spot for the owner of the channel
            if (chat.Members.Count >= _options.MaximumMembers)
            {
                return FriendsChatRegisterResult.Full;
            }

            chat.AddMember(characterId);
            return FriendsChatRegisterResult.Success;
        }

        public async ValueTask<Result> UnregisterMemberAsync(string channelName, uint characterId)
        {
            await Task.CompletedTask;

            var chat = _chatStore[channelName];
            if (chat == null)
            {
                return Result.Fail;
            }

            return chat.RemoveMember(characterId) ? Result.Success : Result.Fail;
        }

        public ValueTask<FriendsChatContext?> FindChatSessionBySessionIdAsync(uint characterId) =>
            new(_chatStore.SingleOrDefault(c => c.Members.Contains(characterId)));

        public async ValueTask<FriendsChatSettingsDto?> FindChatSettingsByNameAsync(string chatName)
        {
            var chat = _chatStore[chatName];
            if (chat == null)
            {
                return null;
            }

            return await _mapper.ProjectTo<FriendsChatSettingsDto>(_unitOfWork.CharacterPreferencesRepository.FindById(chat.OwnerId).AsNoTracking())
                .SingleOrDefaultAsync();
        }

        public async ValueTask<FriendsChatDto.Member?> FindMemberBySessionIdAsync(string chatName, uint characterId)
        {
            var chat = _chatStore[chatName];
            if (chat == null)
            {
                return null;
            }

            var session = _characterStore[characterId];
            if (session == null)
            {
                return null;
            }

            return await CreateMemberDto(chat.OwnerId, characterId, session.WorldId);
        }

        public async ValueTask<FriendsChatDto?> FindChatBySessionIdAsync(uint characterId)
        {
            var chat = _chatStore.SingleOrDefault(c => c.Members.Contains(characterId));
            if (chat == null)
            {
                return null;
            }

            var owner = await _unitOfWork.characterStore.FindByIdAsync(chat.OwnerId)
                .Select(c => new
                {
                    c.Id, c.DisplayName, c.PreviousDisplayName
                })
                .SingleOrDefaultAsync();
            if (owner == null)
            {
                return null;
            }

            var ownerPreferences = await _unitOfWork.CharacterPreferencesRepository.FindById(chat.OwnerId)
                .Select(p => new
                {
                    p.FcRankKick
                })
                .SingleOrDefaultAsync();
            if (ownerPreferences == null)
            {
                return null;
            }

            async IAsyncEnumerable<FriendsChatDto.Member> FindMembers()
            {
                foreach (var memberSessionId in chat.Members)
                {
                    var session = _characterStore[memberSessionId];
                    if (session == null)
                    {
                        continue;
                    }

                    var dto = await CreateMemberDto(owner.Id, session.Id, session.WorldId);
                    if (dto == null)
                    {
                        continue;
                    }

                    yield return dto;
                }
            }

            return new FriendsChatDto()
            {
                ChatName = chat.Name,
                OwnerDisplayName = owner.DisplayName,
                OwnerPreviousDisplayName = owner.PreviousDisplayName,
                Members = await FindMembers().ToListAsync(),
                RankToKick = (FriendsChatRank)ownerPreferences.FcRankKick
            };
        }

        private async ValueTask<FriendsChatDto.Member?> CreateMemberDto(uint ownerId, uint masterId, int worldId)
        {
            var characterMember = await _unitOfWork.characterStore.FindByIdAsync(masterId)
                .Select(c => new
                {
                    c.DisplayName, c.PreviousDisplayName
                })
                .AsNoTracking()
                .SingleOrDefaultAsync();
            if (characterMember == null)
            {
                return null;
            }

            var claims = await _mapper.ProjectTo<FriendsChatDto.Claim>(_unitOfWork.CharacterPermissionsRepository.FindPermissionsByMasterIdAsync(masterId))
                .AsNoTracking()
                .ToListAsync();

            FriendsChatRank rank;
            if (ownerId == masterId)
            {
                rank = FriendsChatRank.Owner;
            }
            else if (claims.Any(p =>
                p.Name == CharactersPermission.PermissionType.GameAdministrator.ToString() ||
                p.Name == CharactersPermission.PermissionType.SystemAdministrator.ToString()))
            {
                rank = FriendsChatRank.Admin;
            }
            else
            {
                var characterContact = await _unitOfWork.CharacterContactsRepository.FindContactByIdAsync(ownerId, masterId)
                    .Select(c => new
                    {
                        c.FcRank
                    })
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
                rank = characterContact == null ? FriendsChatRank.Regular : (FriendsChatRank)characterContact.FcRank;
            }

            return new FriendsChatDto.Member
            {
                DisplayName = characterMember.DisplayName,
                PreviousDisplayName = characterMember.PreviousDisplayName,
                Claims = claims,
                WorldId = worldId,
                Rank = rank
            };
        }
    }
}