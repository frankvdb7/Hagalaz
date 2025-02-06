using System;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public interface IRaidoDispatcher
    {
        Task OnConnectedAsync(RaidoConnectionContext connection);
        Task OnDisconnectedAsync(RaidoConnectionContext connection, Exception? exception);
        Task DispatchMessageAsync(RaidoConnectionContext connection, RaidoMessage message);
    }
}