using System.Threading.Tasks;
using Hagalaz.Services.GameLogon.Services.Model;
using Hagalaz.Services.GameLogon.Store.Model;

namespace Hagalaz.Services.GameLogon.Services
{
    public interface IFriendsChatService
    {
        ValueTask<FriendsChatRegisterResult> RegisterMemberAsync(string ownerDisplayName, uint characterId);
        ValueTask<Result> UnregisterMemberAsync(string chatName, uint characterId);
        ValueTask<FriendsChatDto.Member?> FindMemberBySessionIdAsync(string chatName, uint characterId);
        ValueTask<FriendsChatSettingsDto?> FindChatSettingsByNameAsync(string chatName);
        ValueTask<FriendsChatDto?> FindChatBySessionIdAsync(uint characterId);
        ValueTask<FriendsChatContext?> FindChatSessionBySessionIdAsync(uint characterId);
    }
}