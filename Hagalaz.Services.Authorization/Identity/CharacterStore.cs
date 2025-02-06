using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Authorization.Identity
{
    public class CharacterStore : UserStore<Character, Aspnetrole, HagalazDbContext, uint, Aspnetuserclaim, Aspnetuserrole, Aspnetuserlogin, Aspnetusertoken,
        Aspnetroleclaim>, ICharacterStore
    {
        public CharacterStore(HagalazDbContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }

        public Task<string> GetDisplayNameAsync(Character character, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            return Task.FromResult(character.DisplayName);
        }

        public Task<string?> GetPreviousDisplayNameAsync(Character character, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            return Task.FromResult(character.PreviousDisplayName);
        }

        public Task<DateTimeOffset?> GetLastLoginAsync(Character character, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            return Task.FromResult(character.LastGameLogin);
        }

        public Task<string?> GetLastIpAsync(Character character, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            return Task.FromResult(character.LastIp);
        }
    }
}