using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Messages.Model;
using Hagalaz.Services.GameLogon.Data;
using Hagalaz.Services.GameLogon.Services.Model;
using Hagalaz.Services.GameLogon.Store;
using Hagalaz.Services.GameLogon.Store.Model;
using Hagalaz.Services.GameLogon.Extensions;

namespace Hagalaz.Services.GameLogon.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly UserManager<Character> _characterManager;
        private readonly SignInManager<Character> _signInManager;
        private readonly ICharacterUnitOfWork _unitOfWork;
        private readonly CharacterStore _characterStore;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public CharacterService(
            UserManager<Character> characterManager,
            SignInManager<Character> signInManager,
            ICharacterUnitOfWork unitOfWork,
            CharacterStore characterStore,
            IMapper mapper,
            IPublishEndpoint publishEndpoint)
        {
            _characterManager = characterManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _characterStore = characterStore;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
        }

        public async ValueTask<CharacterSignInResult> SignInAsync(int worldId, string login, string password)
        {
            var character = login.IsEmail() ? await _characterManager.FindByEmailAsync(login) : await _characterManager.FindByNameAsync(login);

            if (character == null)
            {
                // not found
                return CharacterSignInResult.Fail;
            }

            var session = _characterStore[character.Id];
            if (session != null)
            {
                return CharacterSignInResult.AlreadyLoggedOn;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(character, password, true);
            if (result != SignInResult.Success)
            {
                if (result == SignInResult.NotAllowed)
                {
                    return CharacterSignInResult.Disabled;
                }

                if (result == SignInResult.LockedOut)
                {
                    return CharacterSignInResult.LockedOut;
                }

                if (result == SignInResult.Failed)
                {
                    return CharacterSignInResult.Fail;
                }
            }

            var isBanned = await _unitOfWork.CharacterOffenceRepository.FindActiveOffencesByMasterIdAsync(character.Id)
                .AnyAsync(o => o.OffenceType == CharactersOffence.OffenceTypeEnum.Banned);
            if (isBanned)
            {
                return CharacterSignInResult.Disabled;
            }
            
            var principal = await _signInManager.CreateUserPrincipalAsync(character);

            if (!_characterStore.TryAdd(new CharacterSessionContext(character.Id, worldId)))
            {
                return CharacterSignInResult.Fail;
            }

            await NotifyCharacterSignIn(character.Id);

            return CharacterSignInResult.Success(principal);
        }

        private async Task NotifyCharacterSignIn(uint id)
        {
            var character = await FindCharacterBySessionIdAsync(id);
            if (character == null)
            {
                return;
            }

            await _publishEndpoint.Publish(new NotifyCharacterSignIn(new[]
            {
                new NotifyCharacterSignIn.Character(character.Id, character.DisplayName, character.WorldId)
                {
                    PreviousDisplayName = character.PreviousDisplayName,
                }
            }));
        }

        public async ValueTask<Result> SignOutAsync(uint id)
        {
            var session = _characterStore[id];
            if (session == null)
            {
                return Result.NotFound;
            }

            var character = await FindCharacterBySessionIdAsync(id);
            if (character == null)
            {
                return Result.NotFound;
            }

            if (!_characterStore.TryRemove(session))
            {
                return Result.Fail;
            }

            await NotifyCharacterSignOut(character);

            return Result.Success;
        }

        private async Task NotifyCharacterSignOut(CharacterDto character) =>
            await _publishEndpoint.Publish(new NotifyCharacterSignOut(new[]
            {
                new NotifyCharacterSignOut.Character(character.Id, character.DisplayName, character.WorldId ?? 0)
                {
                    PreviousDisplayName = character.PreviousDisplayName
                }
            }));

        public async IAsyncEnumerable<CharacterDto> FindCharactersByWorldIdAsync(int id)
        {
            foreach (var session in _characterStore.Where(s => s.WorldId == id))
            {
                var dto = await ProjectCharacterSessionAsync(session);
                if (dto == null)
                {
                    continue;
                }

                yield return dto;
            }
        }

        public ValueTask<CharacterDto?> FindCharacterBySessionIdAsync(uint id)
        {
            var session = _characterStore[id];
            return session == null ? ValueTask.FromResult<CharacterDto?>(null) : ProjectCharacterSessionAsync(session);
        }

        public async ValueTask<CharacterDto?> FindCharacterByDisplayName(string displayName)
        {
            var dto = await _mapper.ProjectTo<CharacterDto>(_unitOfWork.characterStore.FindByDisplayNameAsync(displayName)).FirstOrDefaultAsync();
            if (dto == null)
            {
                return null;
            }

            var session = _characterStore[dto.Id];
            if (session != null)
            {
                dto = dto with
                {
                    WorldId = session.WorldId
                };
            }

            return await EnrichCharacterWithClaims(dto);
        }

        private async ValueTask<CharacterDto> EnrichCharacterWithClaims(CharacterDto dto)
        {
            var claims = await _mapper.ProjectTo<CharacterDto.Claim>(_unitOfWork.CharacterPermissionsRepository.FindPermissionsByMasterIdAsync(dto.Id))
                .ToListAsync();

            return dto with
            {
                Claims = claims
            };
        }

        private async ValueTask<CharacterDto?> ProjectCharacterSessionAsync(CharacterSessionContext session)
        {
            var dto = await _mapper.ProjectTo<CharacterDto>(_unitOfWork.characterStore.FindByIdAsync(session.Id)).SingleOrDefaultAsync();
            if (dto == null)
            {
                return null;
            }

            dto = await EnrichCharacterWithClaims(dto);

            return dto with
            {
                WorldId = session.WorldId
            };
        }

        public async ValueTask<CharacterSessionContext?> FindSessionByDisplayName(string displayName)
        {
            var character = await _unitOfWork.characterStore.FindByDisplayNameAsync(displayName)
                .Select(c => new
                {
                    c.Id
                })
                .SingleOrDefaultAsync();
            return character == null ? null : _characterStore[character.Id];
        }

        public ValueTask<CharacterSessionContext?> FindSessionByIdAsync(uint id) => ValueTask.FromResult(_characterStore[id]);
    }
}