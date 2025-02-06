using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Authorization.Services;
using Hagalaz.Services.Authorization.Extensions;

namespace Hagalaz.Services.Authorization.Identity
{
    public class CharacterManager : UserManager<Character>
    {
        private readonly ICharacterStore _store;
        private readonly IOffenceService _offenceService;

        public CharacterManager(
            ICharacterStore store,
            IOffenceService offenceService,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<Character> passwordHasher,
            IEnumerable<IUserValidator<Character>> userValidators,
            IEnumerable<IPasswordValidator<Character>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<CharacterManager> logger) : base(store,
            optionsAccessor,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger)
        {
            _store = store;
            _offenceService = offenceService;
        }

        public virtual async Task<Character?> FindByLoginAsync(string login)
        {
            ThrowIfDisposed();
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            return login.IsEmail() ? await FindByEmailAsync(login) : await FindByNameAsync(login);
        }

        /// <summary>
        /// Gets the display name for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose display name should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the display name for the specified <paramref name="user"/>.</returns>
        public virtual async Task<string> GetDisplayNameAsync(Character user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await _store.GetDisplayNameAsync(user, CancellationToken);
        }

        public virtual async Task<string?> GetPreviousDisplayNameAsync(Character user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await _store.GetPreviousDisplayNameAsync(user, CancellationToken);
        }

        /// <summary>
        /// Gets the last login for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose last login should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the last login for the specified <paramref name="user"/>.</returns>
        public virtual async Task<DateTimeOffset?> GetLastLoginAsync(Character user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await _store.GetLastLoginAsync(user, CancellationToken);
        }


        /// <summary>
        /// Gets the last ip for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose last ip should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the last ip for the specified <paramref name="user"/>.</returns>
        public virtual async Task<string?> GetLastIpAsync(Character user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await _store.GetLastIpAsync(user, CancellationToken);
        }

        public virtual async Task<bool> IsBannedAsync(Character user) => await _offenceService.HasActiveBanOffenceAsync(user.Id);
    }
}