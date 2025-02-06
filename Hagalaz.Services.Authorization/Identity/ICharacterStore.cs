using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Authorization.Identity
{
    public interface ICharacterStore : IUserStore<Character>
    {
        Task<string> GetDisplayNameAsync(Character character, CancellationToken cancellationToken);
        Task<string?> GetPreviousDisplayNameAsync(Character character, CancellationToken cancellationToken);
        Task<DateTimeOffset?> GetLastLoginAsync(Character character, CancellationToken cancellationToken);
        Task<string?> GetLastIpAsync(Character character, CancellationToken cancellationToken);
    }
}